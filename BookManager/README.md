# BookManager API

ASP.NET Core Web API для управления мероприятиями.

Проект поддерживает:

- CRUD-операции для мероприятий
- валидацию входных данных
- глобальную обработку ошибок
- фильтрацию событий по названию и датам
- пагинацию результатов
- юнит-тесты для бизнес-логики сервиса

## Требования

- .NET SDK 9.0 или выше

## Запуск проекта

1. Перейти в папку проекта:

```bash
cd BookManager
```

2. Восстановить зависимости:

```bash
dotnet restore
```

3. Собрать проект:

```bash
dotnet build BookManager/BookManager.csproj
```

4. Запустить проект:

```bash
dotnet run --project BookManager
```

## Swagger

После запуска Swagger доступен по адресу:

```text
http://localhost:<port>/swagger
```

или

```text
https://localhost:<port>/swagger
```

Точный адрес будет показан в консоли после запуска приложения.

## Запуск тестов

```bash
dotnet test EventService.Tests/EventService.Tests.csproj
```

## Модель Event

Событие содержит поля:

- `id` — уникальный идентификатор
- `title` — название мероприятия
- `description` — описание мероприятия
- `startAt` — дата и время начала
- `endAt` — дата и время окончания

## API

### GET /events

Возвращает список мероприятий с поддержкой фильтрации и пагинации.

#### Query-параметры

- `title` *(string, optional)* — поиск по названию, частичное совпадение без учёта регистра
- `from` *(DateTime, optional)* — события, начинающиеся не раньше указанной даты
- `to` *(DateTime, optional)* — события, заканчивающиеся не позже указанной даты
- `page` *(int, optional, default = 1)* — номер страницы
- `pageSize` *(int, optional, default = 10)* — размер страницы

#### Примеры

Получить первую страницу событий:

```http
GET /events
```

Поиск по названию:

```http
GET /events?title=meeting
```

Фильтрация по датам:

```http
GET /events?from=2026-05-01&to=2026-06-01
```

Фильтрация с пагинацией:

```http
GET /events?title=team&page=1&pageSize=5
```

---

### GET /events/{id}

Возвращает мероприятие по идентификатору.

Если мероприятие не найдено, возвращается `404 Not Found`.

---

### POST /events

Создаёт новое мероприятие.

При успешном создании возвращается `201 Created`.

#### Пример тела запроса

```json
{
  "title": "Team meeting",
  "description": "Sprint planning",
  "startAt": "2026-04-22T10:00:00",
  "endAt": "2026-04-22T11:00:00"
}
```

---

### PUT /events/{id}

Полностью обновляет мероприятие по идентификатору.

Если мероприятие не найдено, возвращается `404 Not Found`.

#### Пример тела запроса

```json
{
  "title": "Updated meeting",
  "description": "Updated description",
  "startAt": "2026-04-22T12:00:00",
  "endAt": "2026-04-22T13:30:00"
}
```

---

### DELETE /events/{id}

Удаляет мероприятие по идентификатору.

Если мероприятие не найдено, возвращается `404 Not Found`.

## Валидация

Проверяются следующие правила:

- `title` обязателен
- `startAt` обязателен
- `endAt` обязателен
- `endAt` должен быть позже `startAt`

## Формат ошибок

Для ошибок используется единый JSON-формат на основе `ProblemDetails`.

### Пример 400 Bad Request

```json
{
  "status": 400,
  "title": "Validation error",
  "detail": "Page must be greater than 0."
}
```

### Пример 404 Not Found

```json
{
  "status": 404,
  "title": "Resource not found",
  "detail": "Event with id '00000000-0000-0000-0000-000000000000' was not found."
}
```

### Пример 500 Internal Server Error

```json
{
  "status": 500,
  "title": "Internal server error",
  "detail": "An unexpected error occurred."
}
```

## Особенности реализации

- данные хранятся в памяти приложения
- бизнес-логика вынесена в `EventService`
- используется DI
- фильтрация и пагинация реализованы через LINQ
- глобальная обработка ошибок вынесена в middleware
- юнит-тесты написаны с использованием xUnit