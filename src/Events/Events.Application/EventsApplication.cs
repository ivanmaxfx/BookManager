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

public interface IEventRepository
{
    Task<IReadOnlyList<Event>> GetAllAsync(
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

    public EventService(IEventRepository events)
    {
        _events = events;
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
        var item =
            await _events.GetByIdAsync(
                id,
                false,
                cancellationToken)
            ?? throw new NotFoundException(
                $"Event with id '{id}' was not found.");

        return EventDto.From(item);
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
    }
}
