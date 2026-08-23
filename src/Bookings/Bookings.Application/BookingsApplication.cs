using BookManager.Contracts;
using Bookings.Domain;

namespace Bookings.Application;

public sealed record BookingDto(
    Guid Id,
    Guid EventId,
    Guid UserId,
    int SeatCount,
    BookingStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt)
{
    public static BookingDto From(
        Booking booking) =>
        new(
            booking.Id,
            booking.EventId,
            booking.UserId,
            booking.SeatCount,
            booking.Status,
            booking.CreatedAt,
            booking.ProcessedAt);
}

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>>
        GetPendingIdsAsync(
            CancellationToken cancellationToken = default);

    Task AddAsync(
        Booking booking,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}

public interface IBookingEventPublisher
{
    Task PublishConfirmedAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default);
}

public sealed class BookingService
{
    public const int MaxActiveBookingsPerUser =
        10;

    private readonly IBookingRepository _bookings;

    public BookingService(
        IBookingRepository bookings)
    {
        _bookings = bookings;
    }

    public async Task<BookingDto> CreateAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var active =
            await _bookings.CountActiveByUserAsync(
                userId,
                cancellationToken);

        if (active >=
            MaxActiveBookingsPerUser)
        {
            throw new BookingLimitException(
                MaxActiveBookingsPerUser);
        }

        var booking =
            Booking.CreatePending(
                eventId,
                userId);

        await _bookings.AddAsync(
            booking,
            cancellationToken);

        await _bookings.SaveChangesAsync(
            cancellationToken);

        return BookingDto.From(booking);
    }

    public async Task<BookingDto> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var booking =
            await _bookings.GetByIdAsync(
                id,
                false,
                cancellationToken)
            ?? throw new NotFoundException(
                $"Booking with id '{id}' was not found.");

        return BookingDto.From(booking);
    }

    public async Task CancelAsync(
        Guid id,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var booking =
            await _bookings.GetByIdAsync(
                id,
                true,
                cancellationToken)
            ?? throw new NotFoundException(
                $"Booking with id '{id}' was not found.");

        if (!isAdmin &&
            booking.UserId != currentUserId)
        {
            throw new ForbiddenException(
                "You can cancel only your own bookings.");
        }

        booking.Cancel();

        await _bookings.SaveChangesAsync(
            cancellationToken);
    }
}
