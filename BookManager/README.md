# BookManager

ASP.NET Core Web API для управления событиями и бронированиями.

## Технологии

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Npgsql
- Swagger
- xUnit
- EF Core InMemory для юнит-тестов

## Хранение данных

События и бронирования хранятся в PostgreSQL.

Для доступа к базе данных используется `AppDbContext` с двумя наборами:

- `DbSet<Event> Events`;
- `DbSet<Booking> Bookings`.

Маппинг сущностей выполняется через Fluent API:

- `EventConfiguration`;
- `BookingConfiguration`.

В PostgreSQL создаются таблицы:

- `events`;
- `bookings`.

Таблица `bookings` связана с `events` внешним ключом `event_id`.

## Создание схемы базы данных

В пятом спринте схема базы данных создаётся автоматически при запуске приложения:

```csharp
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    database.Database.EnsureCreated();
}
```

Миграции EF Core на этом этапе ещё не используются.

## Запуск PostgreSQL

Для запуска PostgreSQL необходим Docker.

Запустить контейнер:

```bash
docker compose up -d
```

Проверить состояние:

```bash
docker compose ps
```

Проверить готовность PostgreSQL:

```bash
docker compose exec postgres \
  pg_isready \
  -U postgres \
  -d eventapi
```

Остановить контейнер:

```bash
docker compose down
```

Остановить контейнер и удалить данные:

```bash
docker compose down -v
```

## Строка подключения

Стандартная строка подключения находится в файле `BookManager/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres"
  }
}
```

Для другого экземпляра PostgreSQL необходимо изменить:

- `Host`;
- `Port`;
- `Database`;
- `Username`;
- `Password`.

## Сборка проекта

Из корня репозитория:

```bash
dotnet restore BookManager/BookManager.csproj

dotnet build BookManager/BookManager.csproj
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

Swagger после запуска:

```text
http://localhost:5236/swagger
```

При запуске приложение автоматически создаёт таблицы через `EnsureCreated()`, если они ещё не существуют.

## Основные эндпоинты

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

## Работа с событиями

Методы `EventService` выполняются асинхронно и используют `AppDbContext` напрямую.

После добавления, изменения или удаления события вызывается:

```csharp
await context.SaveChangesAsync(cancellationToken);
```

Получение списка событий поддерживает:

- фильтрацию по названию;
- фильтрацию по начальной дате;
- фильтрацию по конечной дате;
- совместное применение фильтров;
- пагинацию.

## Работа с бронированиями

Создание бронирования и уменьшение количества доступных мест сохраняются одним вызовом:

```csharp
await context.SaveChangesAsync(cancellationToken);
```

Обе операции выполняются через один экземпляр `AppDbContext`.

Для защиты от конкурентного овербукинга используется статический `SemaphoreSlim`.

Если свободных мест нет, API возвращает:

```text
409 Conflict
```

## Фоновая обработка бронирований

`BookingBackgroundService` является singleton-сервисом, а `AppDbContext` имеет время жизни `Scoped`.

Поэтому фоновый сервис получает зависимости через:

```csharp
IServiceScopeFactory
```

Для чтения списка ожидающих бронирований создаётся отдельный scope.

Для обработки каждой брони также создаётся отдельный scope и отдельный экземпляр `AppDbContext`.

Параллельная обработка запускается через:

```csharp
await Task.WhenAll(processingTasks);
```

## Проверка PostgreSQL

Посмотреть созданные таблицы:

```bash
docker compose exec postgres \
  psql \
  -U postgres \
  -d eventapi \
  -c '\dt'
```

Посмотреть структуру таблицы событий:

```bash
docker compose exec postgres \
  psql \
  -U postgres \
  -d eventapi \
  -c '\d events'
```

Посмотреть структуру таблицы бронирований:

```bash
docker compose exec postgres \
  psql \
  -U postgres \
  -d eventapi \
  -c '\d bookings'
```

Посмотреть события:

```bash
docker compose exec postgres \
  psql \
  -U postgres \
  -d eventapi \
  -c 'SELECT id, title, total_seats, available_seats FROM events;'
```

Посмотреть бронирования:

```bash
docker compose exec postgres \
  psql \
  -U postgres \
  -d eventapi \
  -c 'SELECT id, event_id, status, created_at, processed_at FROM bookings;'
```

## Юнит-тесты

Юнит-тесты используют пакет:

```text
Microsoft.EntityFrameworkCore.InMemory
```

Для тестов создаётся DI-контейнер с тестовым `AppDbContext`:

```csharp
var databaseName = Guid.NewGuid().ToString();

services.AddDbContext<AppDbContext>(
    options => options.UseInMemoryDatabase(databaseName));
```

Имя базы данных сохраняется в переменной, чтобы все scope одного теста использовали одну базу.

В конкурентных тестах каждый параллельный запрос создаёт собственный scope:

```csharp
using var scope = serviceProvider.CreateScope();

var bookingService = scope.ServiceProvider
    .GetRequiredService<IBookingService>();
```

## Запуск тестов

Запустить все тесты:

```bash
dotnet test \
  EventService.Tests/EventService.Tests.csproj
```

Запустить тесты с подробным выводом:

```bash
dotnet test \
  EventService.Tests/EventService.Tests.csproj \
  --logger "console;verbosity=normal"
```

Запустить только конкурентные тесты:

```bash
dotnet test \
  EventService.Tests/EventService.Tests.csproj \
  --filter "FullyQualifiedName~ConcurrentBookings"
```

## Проверка проекта

Полная проверка из корня репозитория:

```bash
dotnet build BookManager/BookManager.csproj

dotnet test \
  EventService.Tests/EventService.Tests.csproj
```
