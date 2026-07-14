Добавлено ограничение количества мест для событий.

Создание бронирований защищено от овербукинга через lock. При отсутствии мест возвращается 409 Conflict.

Фоновая обработка бронирований выполняется параллельно через Task.WhenAll, а обновление состояния защищено через SemaphoreSlim.

Добавлены тесты на работу мест и конкурентные запросы.

Проверка:

dotnet build BookManager/BookManager.csproj
dotnet test EventService.Tests/EventService.Tests.csproj
