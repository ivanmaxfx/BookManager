using Events.Domain;

namespace Events.Application;

public sealed class EventRequest
{
    public string Title { get; init; } =
        string.Empty;

    public string? Description { get; init; }

    public DateTime StartAt { get; init; }

    public DateTime EndAt { get; init; }

    public int TotalSeats { get; init; }
}

public sealed record EventDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats,
    int AvailableSeats)
{
    public static EventDto From(Event item) =>
        new(
            item.Id,
            item.Title,
            item.Description,
            item.StartAt,
            item.EndAt,
            item.TotalSeats,
            item.AvailableSeats);
}

public static class EventCacheKeys
{
    public const string Top10 =
        "events:top10";

    public static string ById(Guid id) =>
        $"event:{id}";
}

public sealed class EventCacheOptions
{
    public int EventTtlSeconds { get; set; } =
        300;

    public int Top10TtlSeconds { get; set; } =
        60;
}

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

public interface IEventRepository
{
    Task<IReadOnlyList<Event>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>>
        GetTop10PopularAsync(
            CancellationToken cancellationToken = default);

    Task<Event?> GetByIdAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Event item,
        CancellationToken cancellationToken = default);

    void Remove(Event item);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}

public sealed class EventService
{
    private readonly IEventRepository _events;
    private readonly ICacheService _cache;
    private readonly EventCacheOptions _cacheOptions;

    public EventService(
        IEventRepository events,
        ICacheService cache,
        EventCacheOptions cacheOptions)
    {
        _events = events;
        _cache = cache;
        _cacheOptions = cacheOptions;
    }

    public async Task<IReadOnlyList<EventDto>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        var events =
            await _events.GetAllAsync(
                cancellationToken);

        return events
            .Select(EventDto.From)
            .ToList();
    }

    public async Task<EventDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var key =
            EventCacheKeys.ById(id);

        var cached =
            await _cache.GetAsync<EventDto>(
                key,
                cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        var item =
            await _events.GetByIdAsync(
                id,
                false,
                cancellationToken)
            ?? throw new NotFoundException(
                $"Event with id '{id}' was not found.");

        var result =
            EventDto.From(item);

        await _cache.SetAsync(
            key,
            result,
            TimeSpan.FromSeconds(
                _cacheOptions.EventTtlSeconds),
            cancellationToken);

        return result;
    }

    public async Task<IReadOnlyList<EventDto>>
        GetTop10Async(
            CancellationToken cancellationToken = default)
    {
        var cached =
            await _cache.GetAsync<List<EventDto>>(
                EventCacheKeys.Top10,
                cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        var events =
            await _events.GetTop10PopularAsync(
                cancellationToken);

        var result =
            events
                .Select(EventDto.From)
                .ToList();

        await _cache.SetAsync(
            EventCacheKeys.Top10,
            result,
            TimeSpan.FromSeconds(
                _cacheOptions.Top10TtlSeconds),
            cancellationToken);

        return result;
    }

    public async Task<EventDto> CreateAsync(
        EventRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = Event.Create(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats);

        await _events.AddAsync(
            item,
            cancellationToken);

        await _events.SaveChangesAsync(
            cancellationToken);

        // Database first, cache second.
        await _cache.RemoveAsync(
            EventCacheKeys.ById(item.Id),
            cancellationToken);

        return EventDto.From(item);
    }

    public async Task UpdateAsync(
        Guid id,
        EventRequest request,
        CancellationToken cancellationToken = default)
    {
        var item =
            await _events.GetByIdAsync(
                id,
                true,
                cancellationToken)
            ?? throw new NotFoundException(
                $"Event with id '{id}' was not found.");

        item.Update(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats);

        await _events.SaveChangesAsync(
            cancellationToken);

        // Invalidation-on-write.
        await _cache.RemoveAsync(
            EventCacheKeys.ById(id),
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item =
            await _events.GetByIdAsync(
                id,
                true,
                cancellationToken)
            ?? throw new NotFoundException(
                $"Event with id '{id}' was not found.");

        _events.Remove(item);

        await _events.SaveChangesAsync(
            cancellationToken);

        // Invalidation-on-write.
        await _cache.RemoveAsync(
            EventCacheKeys.ById(id),
            cancellationToken);
    }
}
