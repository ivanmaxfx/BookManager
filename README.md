---

# Sprint 10 — Redis caching

В десятом спринте в сервис `Events` добавлено кеширование данных с помощью Redis.

Redis используется для снижения количества обращений к PostgreSQL при чтении часто запрашиваемых данных.

## Что кешируется

Кешируются два сценария:

### Получение события по идентификатору

```http
GET /events/{id}
```

Ключ Redis:

```text
event:{id}
```

Например:

```text
event:36e8ab37-1c07-43fd-a57d-e67384da2af1
```

TTL:

```text
300 секунд
```

Для этого сценария используется паттерн **Cache-Aside**.

Схема работы:

```text
Client
  |
  v
Events API
  |
  v
Redis
  |
  +---- cache hit ----> Response
  |
  +---- cache miss
          |
          v
      PostgreSQL
          |
          v
        Redis
          |
          v
       Response
```

При попадании в кеш запрос в PostgreSQL не выполняется.

При промахе данные загружаются из PostgreSQL, сохраняются в Redis и возвращаются клиенту.

---

### Топ-10 популярных событий

Добавлен публичный endpoint:

```http
GET /events/top
```

Он возвращает до десяти событий с максимальной долей проданных мест.

Популярность рассчитывается по формуле:

```text
(totalSeats - availableSeats) / totalSeats
```

Ключ Redis:

```text
events:top10
```

TTL:

```text
60 секунд
```

Для топ-10 также используется Cache-Aside.

При наличии значения в Redis PostgreSQL не вызывается.

При отсутствии значения в кеше Events Service получает данные из PostgreSQL и сохраняет результат в Redis.

---

## Стратегия инвалидации

Для отдельного события используется стратегия:

```text
Cache-Aside + Invalidation-on-Write
```

При изменении данных сначала выполняется операция с PostgreSQL и только после успешного сохранения удаляется соответствующий ключ Redis.

Порядок операций:

```text
1. Изменить данные.
2. Сохранить изменения в PostgreSQL.
3. Удалить event:{id} из Redis.
```

PostgreSQL остаётся источником истины.

Если выполнение приложения прервётся между сохранением данных в PostgreSQL и удалением кеша, база всё равно останется в актуальном состоянии. Кеш в дальнейшем будет обновлён после следующей успешной инвалидации либо после истечения TTL.

### Создание события

После:

```http
POST /events
```

новое событие сначала сохраняется в PostgreSQL.

После успешного сохранения выполняется удаление ключа:

```text
event:{id}
```

Это исключает возможность использования устаревшего значения для данного события.

### Изменение события

После:

```http
PUT /events/{id}
```

выполняется следующая последовательность:

```text
UPDATE PostgreSQL
        |
        v
SaveChanges
        |
        v
DEL event:{id}
```

Следующий запрос:

```http
GET /events/{id}
```

получит актуальное состояние из PostgreSQL и заново прогреет Redis.

### Удаление события

После:

```http
DELETE /events/{id}
```

событие сначала удаляется из PostgreSQL, после чего удаляется ключ:

```text
event:{id}
```

---

## Стратегия кеширования top-10

Для ключа:

```text
events:top10
```

используется только TTL.

Явная инвалидация top-10 после каждого изменения события или бронирования не выполняется.

Top-10 является рейтинговым агрегатом, для которого небольшое временное устаревание допустимо.

Кроме того, рейтинг потенциально может изменяться после каждого бронирования. Инвалидация кеша после каждой такой операции значительно снизила бы эффективность кеширования.

Поэтому для top-10 выбран TTL:

```text
60 секунд
```

После истечения TTL рейтинг будет автоматически пересчитан при следующем запросе.

---

## Kafka и кеш

Сервис `Events` получает сообщение:

```text
BookingConfirmed
```

из Kafka.

После подтверждения бронирования обработчик уменьшает:

```text
AvailableSeats
```

соответствующего события.

Порядок действий:

```text
BookingConfirmed
        |
        v
Изменение AvailableSeats
        |
        v
PostgreSQL SaveChanges
        |
        v
DEL event:{id}
```

Таким образом, кеш отдельного события инвалидируется не только после HTTP-операций изменения события, но и после асинхронного изменения данных через Kafka.

Кеш:

```text
events:top10
```

при этом не инвалидируется и продолжает жить до окончания TTL.

---

## TTL

TTL вынесены в конфигурацию приложения.

Пример `appsettings.json`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "EventTtlSeconds": 300,
    "Top10TtlSeconds": 60
  }
}
```

Используются разные значения TTL:

| Кеш | TTL | Причина |
|---|---:|---|
| `event:{id}` | 300 секунд | Кеш активно инвалидируется при изменении события |
| `events:top10` | 60 секунд | Рейтинг изменяется чаще и обновляется только по TTL |

---

## Redis и Docker Compose

Redis добавлен в `docker-compose.yml`.

Используется образ:

```text
redis:7.2-alpine
```

Внутри Docker-сети Events Service обращается к Redis по адресу:

```text
redis:6379
```

Конфигурация передаётся через переменные окружения:

```text
Redis__ConnectionString=redis:6379
Redis__EventTtlSeconds=300
Redis__Top10TtlSeconds=60
```

Запуск системы:

```bash
docker compose up -d --build
```

Проверка Redis:

```bash
docker compose exec redis redis-cli ping
```

Ожидаемый ответ:

```text
PONG
```

Посмотреть существующие ключи:

```bash
docker compose exec redis redis-cli keys '*'
```

Пример:

```text
events:top10
event:36e8ab37-1c07-43fd-a57d-e67384da2af1
```

Проверить оставшийся TTL:

```bash
docker compose exec redis redis-cli ttl events:top10
```

---

## Работа при недоступном Redis

Redis является вспомогательным слоем кеширования и не является источником истины.

Если Redis недоступен, Events Service продолжает обслуживать запросы.

Ошибки операций:

```text
GET
SET
DELETE
```

перехватываются и записываются в лог.

Ошибка Redis не должна приводить к ошибке API для клиента.

При недоступном Redis чтение работает следующим образом:

```text
Client
  |
  v
Events API
  |
  v
Redis unavailable
  |
  v
PostgreSQL
  |
  v
Response
```

Таким образом, отказ Redis приводит к увеличению нагрузки на PostgreSQL, но не делает Events API недоступным.

После восстановления Redis кеш начинает заново прогреваться последующими запросами.

---

## Архитектура кеширования

Слой `Events.Application` не зависит от библиотеки `StackExchange.Redis`.

В Application определена абстракция:

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);
}
```

Конкретная реализация находится в:

```text
Events.Infrastructure
```

и использует:

```text
StackExchange.Redis
```

Соединение с Redis зарегистрировано в DI как Singleton.

`ConnectionMultiplexer` является тяжёлым потокобезопасным объектом, поэтому создаётся один раз и переиспользуется на протяжении всего жизненного цикла приложения.

---

## Unit-тесты кеширования

Для проверки логики кеширования добавлен проект:

```text
Events.Application.Tests
```

Проверяются следующие сценарии:

- cache hit для `GET /events/{id}` — репозиторий не вызывается;
- cache miss для `GET /events/{id}` — данные загружаются из репозитория и сохраняются в кеш;
- cache hit для `GET /events/top` — репозиторий не вызывается;
- cache miss для `GET /events/top` — данные загружаются из базы и сохраняются в кеш;
- инвалидация кеша после создания события;
- инвалидация кеша после изменения события;
- инвалидация кеша после удаления события;
- проверка порядка операций: сначала сохранение в PostgreSQL, затем инвалидация Redis.

Запуск тестов:

```bash
dotnet test

---

## Итоговая стратегия Sprint 10

```text
GET /events/{id}
    -> Cache-Aside
    -> event:{id}
    -> TTL 300 секунд

GET /events/top
    -> Cache-Aside
    -> events:top10
    -> TTL 60 секунд

POST / PUT / DELETE
    -> сначала PostgreSQL
    -> затем invalidate event:{id}

BookingConfirmed
    -> изменение PostgreSQL
    -> SaveChanges
    -> invalidate event:{id}

Redis unavailable
    -> warning в лог
    -> fallback на PostgreSQL
    -> API продолжает работать
```

PostgreSQL остаётся источником истины, а Redis используется как дополнительный производительный слой кеширования.

---

## Sprint 11 — Observability

В Sprint 11 во все три микросервиса добавлен единый стек наблюдаемости:

- OpenTelemetry;
- Prometheus;
- Jaeger;
- Grafana;
- Serilog с JSON-логами.

### OpenTelemetry

OpenTelemetry подключён в:

```text
Users Service
Events Service
Bookings Service
```

Для каждого сервиса собираются:

```text
HTTP traces
HTTP metrics
outgoing HTTP traces
Entity Framework Core / PostgreSQL traces
.NET runtime metrics
```

Имена сервисов:

```text
users-service
events-service
bookings-service
```

Трейсы отправляются по OTLP gRPC в Jaeger.

Локальный endpoint:

```text
http://localhost:4317
```

В Docker:

```text
http://jaeger:4317
```

### Prometheus

Каждый API предоставляет:

```text
/metrics
```

Локальные адреса:

```text
http://localhost:5001/metrics
http://localhost:5002/metrics
http://localhost:5003/metrics
```

Prometheus собирает метрики с:

```text
users:8080
events:8080
bookings:8080
```

Конфигурация находится в:

```text
prometheus.yml
```

Prometheus UI:

```text
http://localhost:9090
```

### Jaeger

Jaeger получает распределённые трейсы через OTLP.

Jaeger UI:

```text
http://localhost:16686
```

В UI доступны сервисы:

```text
users-service
events-service
bookings-service
```

HTTP-запросы создают HTTP spans, а операции через Entity Framework Core создают database spans.

### JSON logging

Все сервисы используют Serilog.

Логи выводятся в stdout в структурированном JSON-формате через:

```text
CompactJsonFormatter
```

Это позволяет в дальнейшем передавать их в централизованную систему сбора логов без парсинга обычного текстового формата.

### Grafana

Grafana UI:

```text
http://localhost:3000
```

Данные для входа:

```text
login: admin
password: admin
```

Prometheus datasource и dashboard настраиваются автоматически через Grafana provisioning.

Файлы:

```text
grafana/provisioning/datasources/prometheus.yml
grafana/provisioning/dashboards/bookmanager.yml
grafana/dashboards/bookmanager-observability.json
```

Dashboard:

```text
BookManager Observability
```

содержит:

```text
Latency p50 / p95 / p99
Throughput (RPS)
Active HTTP requests
5xx error rate
```

Используются метрики:

```text
http_server_request_duration_seconds
http_server_request_duration_seconds_count
http_server_active_requests
```

### Запуск observability stack

```bash
docker compose up -d --build
```

Проверка контейнеров:

```bash
docker compose ps
```

Проверка метрик:

```bash
curl http://localhost:5001/metrics
curl http://localhost:5002/metrics
curl http://localhost:5003/metrics
```

Проверка Prometheus:

```text
http://localhost:9090/targets
```

Все три target должны иметь состояние:

```text
UP
```

Проверка Jaeger:

```text
http://localhost:16686
```

Проверка Grafana:

```text
http://localhost:3000
```

### Порты

| Компонент | Порт | Назначение |
|---|---:|---|
| Users API | 5001 | REST API / metrics |
| Events API | 5002 | REST API / metrics |
| Bookings API | 5003 | REST API / metrics |
| Prometheus | 9090 | Metrics UI |
| Jaeger | 16686 | Trace UI |
| Jaeger OTLP | 4317 | OTLP gRPC |
| Grafana | 3000 | Dashboard UI |

Таким образом, Prometheus отвечает за хранение и запрос метрик, Jaeger — за распределённые трейсы, Grafana — за визуализацию технических показателей, а Serilog предоставляет единый структурированный формат логов.
