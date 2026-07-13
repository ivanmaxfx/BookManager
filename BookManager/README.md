# BookManager

ASP.NET Core Web API для управления мероприятиями и бронированиями.

## Что добавлено в третьем спринте

Добавлена сущность `Booking` со следующими полями:

- `Id`
- `EventId`
- `Status`
- `CreatedAt`
- `ProcessedAt`

Добавлены статусы бронирования:

- `Pending`
- `Confirmed`
- `Rejected`

Реализованы:

- хранение бронирований в памяти;
- `BookingService` и интерфейс `IBookingService`;
- создание брони для существующего мероприятия;
- получение брони по идентификатору;
- фоновая обработка бронирований через `BackgroundService`;
- искусственная задержка перед обработкой;
- перевод брони из `Pending` в `Confirmed`;
- заполнение `ProcessedAt`;
- обработка отсутствующих мероприятий и бронирований через `404 Not Found`;
- Swagger для новых эндпоинтов.

## Новые эндпоинты

### Создание бронирования

```http
POST /events/{id}/book
```

Возвращает:

```text
202 Accepted
```

В заголовке `Location` возвращается ссылка:

```text
/bookings/{bookingId}
```

Новая бронь создаётся со статусом `Pending`.

### Получение бронирования

```http
GET /bookings/{id}
```

Возвращает текущее состояние брони.

Через несколько секунд после создания статус меняется с `Pending` на `Confirmed`.

## Запуск проекта

Требуется .NET SDK 9.0.

Из корня репозитория:

```bash
dotnet restore BookManager/BookManager.csproj
dotnet build BookManager/BookManager.csproj
dotnet run --project BookManager/BookManager.csproj
```

Для запуска :

```bash
dotnet run --project BookManager/BookManager.csproj --urls http://0.0.0.0:5236
```

Swagger:

```text
http://127.0.0.1:5236/swagger
```

## Запуск тестов

```bash
dotnet test EventService.Tests/EventService.Tests.csproj
```

## Проверка через Swagger

1. Создать мероприятие:

```http
POST /events
```

2. Скопировать его `id`.

3. Создать бронирование:

```http
POST /events/{eventId}/book
```

4. Проверить статус бронирования:

```http
GET /bookings/{bookingId}
```

Сразу после создания статус должен быть `Pending`.

Через несколько секунд повторный запрос должен вернуть статус `Confirmed` и заполненное поле `ProcessedAt`.

