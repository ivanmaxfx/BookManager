# BookManager — Sprint 9

BookManager в Sprint 9 разделён на три независимых микросервиса с асинхронным взаимодействием через Apache Kafka.

## Архитектура

```text
                   +----------------+
                   | Users / Auth   |
                   | PostgreSQL     |
                   | :5001          |
                   +----------------+
                          |
                          | JWT
                          v

+----------------+     Kafka      +----------------+
| Bookings       | -------------> | Events         |
| PostgreSQL     | BookingConfirmed| PostgreSQL     |
| :5003          |                | :5002          |
+----------------+                +----------------+
```

Сервисы не вызывают друг друга по HTTP.

## Сервисы

### Users / Auth

Порт:

```text
5001
```

База:

```text
usersdb
```

Функции:

- регистрация;
- login;
- SHA-256 password hashing;
- выдача JWT.

Endpoints:

```text
POST /auth/register
POST /auth/login
```

### Events

Порт:

```text
5002
```

База:

```text
eventsdb
```

Функции:

- CRUD событий;
- учёт доступных мест;
- Kafka consumer `BookingConfirmed`.

Endpoints:

```text
GET    /events
GET    /events/{id}
POST   /events
PUT    /events/{id}
DELETE /events/{id}
```

POST, PUT и DELETE доступны только роли `Admin`.

### Bookings

Порт:

```text
5003
```

База:

```text
bookingsdb
```

Функции:

- создание броней;
- получение брони;
- отмена брони;
- фоновое подтверждение;
- публикация `BookingConfirmed`.

Endpoints:

```text
POST   /events/{eventId}/book
GET    /bookings/{id}
DELETE /bookings/{id}
```

Все endpoints требуют JWT.

## Clean Architecture

Каждый сервис разделён на:

```text
Domain
Application
Infrastructure
Presentation
```

Направление зависимостей:

```text
Presentation -> Application
Presentation -> Infrastructure

Infrastructure -> Application
Infrastructure -> Domain

Application -> Domain

Domain -> nothing
```

## Shared contracts

Общий проект:

```text
src/BookManager.Contracts
```

Содержит:

```text
KafkaTopics.BookingConfirmed
BookingConfirmed
```

Контракт:

```text
BookingId
EventId
UserId
SeatCount
ConfirmedAt
```

## BookingConfirmed flow

```text
1. User создаёт booking
                |
                v
2. Bookings сохраняет Pending
                |
                v
3. Background worker подтверждает booking
                |
                v
4. Status=Confirmed сохраняется в bookingsdb
                |
                v
5. Bookings публикует BookingConfirmed в Kafka
                |
                v
6. Events consumer получает сообщение
                |
                v
7. Events проверяет идемпотентность по BookingId
                |
                v
8. Events уменьшает AvailableSeats
                |
                v
9. Изменение сохраняется в eventsdb
```

Bookings не обращается к Events по HTTP.

## Kafka

Topic:

```text
booking-confirmed
```

Producer:

```text
Bookings.Infrastructure
```

Producer зарегистрирован singleton и реализует `IDisposable`.

Ключ Kafka сообщения:

```text
EventId
```

Поэтому сообщения одного события попадают в один partition и сохраняют порядок.

Consumer:

```text
Events.Infrastructure
```

Consumer работает как `BackgroundService`.

Для каждого события создаётся отдельный DI scope.

Events также создаёт Kafka topic при запуске, если он ещё не существует.

## Идемпотентность

Events хранит обработанные `BookingId` в таблице:

```text
processed_booking_events
```

Повторная доставка одного `BookingConfirmed` не уменьшает количество мест второй раз.

## Eventual consistency

Изменение брони сначала сохраняется в Bookings DB.

Только после этого публикуется интеграционное событие.

Events изменяет собственную БД независимо после получения сообщения Kafka.

## JWT

JWT выдаёт только Users.

Во всех трёх сервисах используются одинаковые:

```text
Secret
Issuer
Audience
```

Events и Bookings валидируют токен самостоятельно.

## Swagger

```text
Users:
http://localhost:5001/swagger

Events:
http://localhost:5002/swagger

Bookings:
http://localhost:5003/swagger
```

Во всех Swagger настроен Bearer JWT.

## Docker

Система состоит из:

```text
Zookeeper
Kafka

users-db
events-db
bookings-db

users
events
bookings
```

Запуск:

```bash
docker compose up -d --build
```

Состояние:

```bash
docker compose ps
```

Логи:

```bash
docker compose logs -f
```

Остановка:

```bash
docker compose down
```

Остановка с удалением локальных данных PostgreSQL:

```bash
docker compose down -v
```

Последняя команда удаляет Docker volumes и все локальные данные трёх БД.

## Build

```bash
dotnet restore BookManager.sln
dotnet build BookManager.sln
```

## EF Core migrations

### Users

```bash
dotnet tool run dotnet-ef database update \
  --project src/Users/Users.Infrastructure/Users.Infrastructure.csproj \
  --startup-project src/Users/Users.Presentation/Users.Presentation.csproj
```

### Events

```bash
dotnet tool run dotnet-ef database update \
  --project src/Events/Events.Infrastructure/Events.Infrastructure.csproj \
  --startup-project src/Events/Events.Presentation/Events.Presentation.csproj
```

### Bookings

```bash
dotnet tool run dotnet-ef database update \
  --project src/Bookings/Bookings.Infrastructure/Bookings.Infrastructure.csproj \
  --startup-project src/Bookings/Bookings.Presentation/Bookings.Presentation.csproj
```

В Docker migrations автоматически применяются при старте каждого сервиса.

## Порты

| Компонент | Порт |
|---|---:|
| Users API | 5001 |
| Events API | 5002 |
| Bookings API | 5003 |
| Kafka | 9092 |
| Users PostgreSQL | 5433 |
| Events PostgreSQL | 5434 |
| Bookings PostgreSQL | 5435 |
