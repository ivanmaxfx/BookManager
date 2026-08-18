# BookManager

BookManager — ASP.NET Core Web API для управления мероприятиями и бронированиями.

Текущая версия проекта реализует **Clean Architecture**, PostgreSQL, Entity Framework Core, JWT-аутентификацию и ролевую авторизацию.

## Технологии

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT Bearer Authentication
- Swagger / OpenAPI
- xUnit
- Testcontainers
- Docker

## Архитектура

Проект разделён на четыре production-слоя:

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

Зависимости:

```text
Application    -> Domain
Infrastructure -> Application
Infrastructure -> Domain
Presentation   -> Application
Presentation   -> Infrastructure
```

### BookManager.Domain

Содержит предметную модель:

- `Event`
- `Booking`
- `User`
- `BookingStatus`
- `UserRole`
- доменные исключения

Domain не зависит от ASP.NET Core, Entity Framework Core или Infrastructure.

### BookManager.Application

Содержит бизнес-логику приложения:

- `EventService`
- `BookingService`
- `AuthService`
- DTO
- интерфейсы репозиториев
- `IUnitOfWork`
- `IPasswordHasher`
- `IJwtTokenGenerator`

Application зависит только от Domain.

### BookManager.Infrastructure

Содержит инфраструктурные реализации:

- `AppDbContext`
- EF Core mappings
- EF Core migrations
- `EventRepository`
- `BookingRepository`
- `UserRepository`
- `UnitOfWork`
- SHA-256 password hashing
- JWT generation

### BookManager.Presentation

Содержит:

- ASP.NET Core Controllers
- JWT authentication
- role-based authorization
- Swagger
- middleware обработки ошибок
- Dependency Injection
- background services
- composition root

---

# Sprint 8

В Sprint 8 в BookManager добавлены пользователи, JWT-аутентификация и разграничение доступа.

## Пользователи

Добавлена сущность:

```text
User
```

Пользователь содержит:

- `Id`
- `Login`
- `PasswordHash`
- `Role`

Поддерживаются роли:

```text
User
Admin
```

## Бронирования

`Booking` теперь связан с пользователем:

```text
Booking
├── EventId
└── UserId
```

Добавлен статус:

```text
Cancelled
```

## Бизнес-правила

В приложении действуют следующие правила:

- нельзя бронировать уже начавшееся мероприятие;
- пользователь может иметь максимум 10 активных бронирований;
- активными считаются `Pending` и `Confirmed`;
- лимит считается отдельно для каждого пользователя;
- пользователь может отменить только собственную бронь;
- администратор может отменить любую бронь;
- отменённая бронь получает статус `Cancelled`;
- после отмены место возвращается мероприятию.

## Авторизация

Права:

| Действие | User | Admin |
|---|---:|---:|
| Просмотр событий | Да | Да |
| Создание события | Нет | Да |
| Изменение события | Нет | Да |
| Удаление события | Нет | Да |
| Создание бронирования | Да | Да |
| Просмотр бронирования | Да | Да |
| Отмена своей брони | Да | Да |
| Отмена чужой брони | Нет | Да |

Для защищённых endpoints используется:

```http
Authorization: Bearer <JWT>
```

Без JWT защищённые методы возвращают:

```text
401 Unauthorized
```

При недостаточных правах:

```text
403 Forbidden
```

---

# Authentication API

## Регистрация

```http
POST /auth/register
```

Пример:

```json
{
  "login": "user1",
  "password": "password",
  "role": "User"
}
```

Для обычного пользователя:

```json
{
  "login": "user1",
  "password": "password"
}
```

Роль по умолчанию:

```text
User
```

## Авторизация

```http
POST /auth/login
```

Запрос:

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

---

# Swagger

После запуска приложения Swagger доступен по адресу:

```text
http://localhost:<port>/swagger
```

Для работы с защищёнными endpoints:

1. Выполнить `POST /auth/register`.
2. Выполнить `POST /auth/login`.
3. Скопировать `token`.
4. Нажать **Authorize** в Swagger.
5. Вставить JWT.
6. Выполнять защищённые запросы.

---

# JWT configuration

Настройки находятся в:

```text
BookManager.Presentation/appsettings.json
```

Пример:

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

Секрет в репозитории предназначен только для локальной разработки.

В production JWT Secret необходимо передавать через безопасное хранилище или переменную окружения:

```bash
export Jwt__Secret="very-long-production-secret"
```

---

# Password hashing

Пароли не сохраняются в открытом виде.

Для учебной реализации Sprint 8 используется:

```text
SHA-256
```

В базе хранится только `PasswordHash`.

---

# PostgreSQL и Entity Framework Core

Контекст:

```text
BookManager.Infrastructure/DataAccess/AppDbContext.cs
```

Миграции:

```text
BookManager.Infrastructure/DataAccess/Migrations
```

Sprint 8 добавляет:

- таблицу `users`;
- уникальный индекс для login;
- `user_id` в `bookings`;
- внешний ключ `bookings -> users`.

## Применение миграций

```bash
dotnet tool restore

dotnet tool run dotnet-ef database update \
  --project BookManager.Infrastructure/BookManager.Infrastructure.csproj \
  --startup-project BookManager.Presentation/BookManager.Presentation.csproj
```

---

# Сборка

Из корня репозитория:

```bash
dotnet restore BookManager.Presentation/BookManager.sln

dotnet build BookManager.Presentation/BookManager.sln
```

---

# Запуск

PostgreSQL должен быть доступен согласно connection string.

```bash
dotnet run \
  --project BookManager.Presentation/BookManager.Presentation.csproj
```

---

# Тесты

Unit tests:

```bash
dotnet test EventService.Tests/EventService.Tests.csproj
```

Все тесты solution:

```bash
dotnet test BookManager.Presentation/BookManager.sln
```

Integration tests используют PostgreSQL через Testcontainers, поэтому Docker должен быть запущен:

```bash
docker info
```

---

# Структура проекта

```text
BookManager/
├── BookManager.Domain/
├── BookManager.Application/
├── BookManager.Infrastructure/
├── BookManager.Presentation/
├── BookManager.Presentation.Tests/
├── EventService.Tests/
├── EventApi.IntegrationTests/
└── README.md
```
