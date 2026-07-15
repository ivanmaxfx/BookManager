# BookManager

ASP.NET Core Web API для управления событиями и бронированиями.

## Технологии

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core 9
- PostgreSQL
- Npgsql
- EF Core migrations
- Swagger
- xUnit
- Testcontainers for .NET
- EF Core InMemory для юнит-тестов

## Архитектура

Приложение разделено на несколько основных уровней:

- контроллеры принимают HTTP-запросы;
- сервисы содержат бизнес-логику;
- репозитории выполняют операции с данными;
- `UnitOfWork` сохраняет изменения через единый `AppDbContext`;
- фоновый сервис обрабатывает ожидающие бронирования;
- PostgreSQL используется как постоянное хранилище.

Сервисы не обращаются к `AppDbContext` напрямую.

## Сущности

### Event

Событие содержит:

- идентификатор;
- название;
- описание;
- дату начала;
- дату окончания;
- общее количество мест;
- количество свободных мест;
- коллекцию бронирований.

### Booking

Бронирование содержит:

- идентификатор;
- идентификатор события;
- статус;
- время создания;
- время обработки;
- ссылку на событие.

Статус бронирования хранится в PostgreSQL как строка.

## База данных

Контекст базы данных:

```csharp
public DbSet<Event> Events => Set<Event>();

public DbSet<Booking> Bookings => Set<Booking>();
```

Таблицы:

```text
events
bookings
```

Между ними настроена связь:

```text
bookings.event_id → events.id
```

## Миграции

Схема базы данных управляется через EF Core migrations.

При запуске приложения выполняется:

```csharp
database.Database.Migrate();
```

Приложение автоматически применяет все ещё не применённые миграции.

Создана начальная миграция:

```text
InitialCreate
```

Она создаёт:

- таблицу `events`;
- таблицу `bookings`;
- первичные ключи;
- внешний ключ бронирования на событие;
- индексы;
- ограничения количества мест;
- таблицу `__EFMigrationsHistory`.

## Работа с миграциями

Восстановить локальные инструменты:

```bash
dotnet tool restore
```

Посмотреть список миграций:

```bash
dotnet tool run dotnet-ef migrations list \
  --project BookManager/BookManager.csproj \
  --startup-project BookManager/BookManager.csproj
```

Добавить новую миграцию:

```bash
dotnet tool run dotnet-ef migrations add MigrationName \
  --project BookManager/BookManager.csproj \
  --startup-project BookManager/BookManager.csproj \
  --output-dir DataAccess/Migrations
```

Проверить изменения модели, для которых миграция ещё не создана:

```bash
dotnet tool run dotnet-ef migrations has-pending-model-changes \
  --project BookManager/BookManager.csproj \
  --startup-project BookManager/BookManager.csproj
```

## Репозитории

### EventRepository

Поддерживает:

- добавление события;
- получение события по идентификатору;
- проверку существования;
- получение количества событий;
- фильтрацию;
- пагинацию;
- обновление;
- удаление.

### BookingRepository

Поддерживает:

- добавление бронирования;
- получение по идентификатору;
- получение ожидающих бронирований;
- получение идентификаторов ожидающих бронирований;
- обновление;
- удаление.

## Unit of Work

`IUnitOfWork` выполняет единое сохранение изменений:

```csharp
Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default);
```

Это позволяет сохранить уменьшение количества свободных мест и создание бронирования одной операцией.

## Конкурентное бронирование

Создание бронирований защищено статическим `SemaphoreSlim`.

Алгоритм:

1. получить событие с отслеживанием изменений;
2. проверить наличие свободных мест;
3. уменьшить количество свободных мест;
4. создать бронирование со статусом `Pending`;
5. сохранить событие и бронирование одним `SaveChangesAsync()`.

Если свободных мест нет, API возвращает:

```text
409 Conflict
```

## Фоновая обработка

`BookingBackgroundService` получает scoped-зависимости через:

```csharp
IServiceScopeFactory
```

Для каждой параллельно обрабатываемой брони создаётся отдельный scope.

Обработка запускается через:

```csharp
await Task.WhenAll(processingTasks);
```

Успешное бронирование получает статус:

```text
Confirmed
```

При ошибке обработки бронирование отклоняется, а место возвращается событию.

## Запуск PostgreSQL

Запустить контейнеры:

```bash
docker compose up -d
```

Проверить состояние:

```bash
docker compose ps
```

Остановить контейнеры:

```bash
docker compose down
```

Удалить контейнеры вместе с локальными данными:

```bash
docker compose down -v
```

## Сборка

Из корня репозитория:

```bash
dotnet restore BookManager/BookManager.sln

dotnet build BookManager/BookManager.sln
```

## Запуск приложения

```bash
dotnet run \
  --project BookManager/BookManager.csproj
```

Запуск на определённом адресе:

```bash
dotnet run \
  --project BookManager/BookManager.csproj \
  --urls http://0.0.0.0:5236
```

Swagger:

```text
http://localhost:5236/swagger
```

## Эндпоинты

### События

```text
GET    /events
GET    /events/{id}
POST   /events
PUT    /events/{id}
DELETE /events/{id}
```

### Бронирования

```text
POST   /events/{id}/book
GET    /bookings/{id}
```

## Юнит-тесты

Юнит-тесты используют:

```text
Microsoft.EntityFrameworkCore.InMemory
```

Каждый тест создаёт отдельную базу данных с уникальным именем.

Запуск:

```bash
dotnet test \
  EventService.Tests/EventService.Tests.csproj
```

## Интеграционные тесты

Интеграционные тесты используют настоящий PostgreSQL, который запускается через Testcontainers.

Проверяются:

- применение начальной миграции;
- создание таблиц;
- наличие внешнего ключа;
- добавление событий;
- чтение событий;
- фильтрация и пагинация;
- обновление событий;
- удаление событий;
- добавление бронирований;
- чтение бронирований;
- получение ожидающих бронирований;
- обновление статуса;
- удаление бронирований.

Для запуска интеграционных тестов Docker должен быть доступен текущему пользователю:

```bash
docker info
```

Запуск:

```bash
dotnet test \
  EventApi.IntegrationTests/EventApi.IntegrationTests.csproj
```

Не следует запускать тесты через `sudo`.

## Полная проверка

```bash
dotnet build BookManager/BookManager.sln

dotnet test \
  EventService.Tests/EventService.Tests.csproj

dotnet test \
  EventApi.IntegrationTests/EventApi.IntegrationTests.csproj

git diff --check
```
