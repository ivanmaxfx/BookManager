# BookManager

BookManager — ASP.NET Core Web API для управления мероприятиями и бронированиями.

Проект построен по принципам **Clean Architecture** и разделён на четыре production-слоя:

- `BookManager.Domain`
- `BookManager.Application`
- `BookManager.Infrastructure`
- `BookManager.Presentation`

## Архитектура

Зависимости между слоями направлены внутрь:

```text
BookManager.Presentation
        |
        +------> BookManager.Application
        |
        +------> BookManager.Infrastructure
                         |
                         +------> BookManager.Application
                         |
                         +------> BookManager.Domain

BookManager.Application
        |
        +------> BookManager.Domain

BookManager.Domain
        |
        +------> не зависит от остальных слоёв
```

Главное правило архитектуры:

```text
Domain <- Application <- Infrastructure
             ^
             |
        Presentation
```

`Presentation` также зависит от `Infrastructure`, поскольку является composition root и связывает интерфейсы Application с конкретными инфраструктурными реализациями.

---

## BookManager.Domain

Слой предметной области.

Содержит доменные сущности, перечисления и исключения:

- `Event`
- `Booking`
- `BookingStatus`
- `ValidationException`
- `NotFoundException`
- `NoAvailableSeatsException`

`Domain` не зависит от:

- ASP.NET Core;
- Entity Framework Core;
- PostgreSQL;
- Infrastructure;
- Presentation.

В этом слое находится основная предметная модель приложения.

---

## BookManager.Application

Слой бизнес-логики и сценариев использования.

Содержит:

- `EventService`
- `BookingService`
- `IEventService`
- `IBookingService`
- DTO
- `IEventRepository`
- `IBookingRepository`
- `IUnitOfWork`

Интерфейсы репозиториев и Unit of Work являются портами, через которые Application взаимодействует с внешней инфраструктурой.

`Application` зависит только от `Domain` и не знает:

- какой используется ORM;
- какая используется СУБД;
- как устроен HTTP API;
- как реализованы репозитории.

---

## BookManager.Infrastructure

Инфраструктурный слой.

Содержит реализации портов из Application:

- `AppDbContext`
- Entity Framework Core configurations
- EF Core migrations
- `EventRepository`
- `BookingRepository`
- `UnitOfWork`

Для доступа к данным используются:

- Entity Framework Core
- Npgsql
- PostgreSQL

Infrastructure зависит от:

- `BookManager.Application`
- `BookManager.Domain`

Application при этом не зависит от Infrastructure.

---

## BookManager.Presentation

HTTP-слой приложения и composition root.

Содержит:

- ASP.NET Core controllers
- middleware обработки ошибок
- Swagger
- конфигурацию Dependency Injection
- hosted/background services
- точку запуска приложения

Контроллеры остаются тонкими:

```text
HTTP request
    |
    v
Controller
    |
    v
Application Service
    |
    v
Repository Port
```

Контроллеры не обращаются напрямую к `AppDbContext` и конкретным реализациям репозиториев.

Middleware преобразует доменные исключения в HTTP-ответы:

- `ValidationException` → `400 Bad Request`
- `NotFoundException` → `404 Not Found`
- `NoAvailableSeatsException` → `409 Conflict`

---

## Структура репозитория

```text
BookManager/
├── BookManager.Domain/
├── BookManager.Application/
├── BookManager.Infrastructure/
├── BookManager.Presentation/
├── EventService.Tests/
├── EventApi.IntegrationTests/
└── README.md
```

---

## Требования

Для работы проекта необходимы:

- .NET 9 SDK
- PostgreSQL
- Docker для интеграционных тестов через Testcontainers

Проверить установленную версию .NET:

```bash
dotnet --version
```

Проверить Docker:

```bash
docker info
```

---

## Восстановление зависимостей

Из корня репозитория:

```bash
dotnet restore BookManager.Presentation/BookManager.sln
```

---

## Сборка

```bash
dotnet build BookManager.Presentation/BookManager.sln
```

---

## Запуск приложения

Перед запуском должен быть доступен PostgreSQL, указанный в connection string приложения.

Запуск:

```bash
dotnet run \
  --project BookManager.Presentation/BookManager.Presentation.csproj
```

После успешного запуска ASP.NET Core выведет адрес приложения в консоль.

Swagger доступен по пути:

```text
/swagger
```

Например:

```text
http://localhost:<port>/swagger
```

---

## Тесты

Запуск всех тестовых проектов, добавленных в solution:

```bash
dotnet test \
  BookManager.Presentation/BookManager.sln
```

Отдельный запуск unit tests:

```bash
dotnet test \
  EventService.Tests/EventService.Tests.csproj
```

Отдельный запуск integration tests:

```bash
dotnet test \
  EventApi.IntegrationTests/EventApi.IntegrationTests.csproj
```

Интеграционные тесты используют PostgreSQL через Testcontainers, поэтому Docker должен быть запущен.

---

## Entity Framework Core

`AppDbContext`, конфигурации и миграции находятся в проекте:

```text
BookManager.Infrastructure
```

Startup project:

```text
BookManager.Presentation
```

### Восстановить локальные .NET tools

```bash
dotnet tool restore
```

### Посмотреть список миграций

```bash
dotnet tool run dotnet-ef migrations list \
  --project BookManager.Infrastructure/BookManager.Infrastructure.csproj \
  --startup-project BookManager.Presentation/BookManager.Presentation.csproj
```

### Создать новую миграцию

```bash
dotnet tool run dotnet-ef migrations add MigrationName \
  --project BookManager.Infrastructure/BookManager.Infrastructure.csproj \
  --startup-project BookManager.Presentation/BookManager.Presentation.csproj \
  --output-dir DataAccess/Migrations
```

### Применить миграции

```bash
dotnet tool run dotnet-ef database update \
  --project BookManager.Infrastructure/BookManager.Infrastructure.csproj \
  --startup-project BookManager.Presentation/BookManager.Presentation.csproj
```

---

## Направление зависимостей

Итоговая схема production-проектов:

```text
BookManager.Domain
        ^
        |
BookManager.Application
        ^
        |
BookManager.Infrastructure

BookManager.Presentation
        |
        +----> BookManager.Application
        |
        +----> BookManager.Infrastructure
```

Таким образом:

- Domain не знает ни о каких внешних слоях;
- Application содержит бизнес-логику и определяет порты;
- Infrastructure реализует эти порты;
- Presentation отвечает за HTTP и связывает зависимости приложения.


---

## Sprint 8: JWT-аутентификация

В приложении используются JWT-аутентификация и ролевая авторизация.

Роли:

| Действие | User | Admin |
|---|---:|---:|
| Просмотр событий | Да | Да |
| Создание события | Нет | Да |
| Редактирование события | Нет | Да |
| Удаление события | Нет | Да |
| Создание брони | Да | Да |
| Просмотр брони | Да | Да |
| Отмена своей брони | Да | Да |
| Отмена чужой брони | Нет | Да |

### Регистрация

`POST /auth/register`

```json
{
  "login": "user1",
  "password": "password",
  "role": "User"
}
```

Поле `role` необязательно. По умолчанию используется `User`.
Для тестирования разрешена регистрация с ролью `Admin`.

### Получение JWT

`POST /auth/login`

```json
{
  "login": "user1",
  "password": "password"
}
```

Ответ:

```json
{
  "token": "<jwt-token>"
}
```

### JWT в Swagger

1. Зарегистрировать пользователя через `/auth/register`.
2. Получить токен через `/auth/login`.
3. Нажать **Authorize**.
4. Вставить полученный JWT.
5. Выполнять защищённые запросы.

### JWT configuration

Настройки находятся в
`BookManager.Presentation/appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "...",
    "Issuer": "BookManager",
    "Audience": "BookManager.Client",
    "LifetimeMinutes": 60
  }
}
```

Секрет из репозитория предназначен только для локальной разработки.

Для production необходимо использовать безопасный секрет,
например через переменную окружения:

```bash
export Jwt__Secret="very-long-production-secret"
```

### Бизнес-правила бронирования

- нельзя бронировать уже начавшееся событие;
- максимум 10 активных броней на пользователя;
- активные статусы: `Pending` и `Confirmed`;
- пользователь отменяет только собственную бронь;
- администратор может отменить любую бронь;
- отменённая бронь получает статус `Cancelled`;
- при отмене место возвращается событию;
- пароли хранятся в виде SHA-256 хеша.
