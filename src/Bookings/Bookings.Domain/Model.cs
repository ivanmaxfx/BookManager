namespace Bookings.Domain;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Cancelled
}

public sealed class Booking
{
    private Booking()
    {
    }

    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public Guid UserId { get; private set; }

    public int SeatCount { get; private set; }

    public BookingStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public static Booking CreatePending(
        Guid eventId,
        Guid userId,
        int seatCount = 1)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainValidationException(
                "EventId is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainValidationException(
                "UserId is required.");
        }

        if (seatCount <= 0)
        {
            throw new DomainValidationException(
                "Seat count must be greater than zero.");
        }

        return new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            SeatCount = seatCount,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
        {
            throw new DomainValidationException(
                "Only pending booking can be confirmed.");
        }

        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new DomainValidationException(
                "Booking is already cancelled.");
        }

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}

public sealed class DomainValidationException :
    Exception
{
    public DomainValidationException(string message)
        : base(message)
    {
    }
}

public sealed class NotFoundException :
    Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}

public sealed class ForbiddenException :
    Exception
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}

public sealed class BookingLimitException :
    Exception
{
    public BookingLimitException(int limit)
        : base(
            $"Active booking limit of {limit} has been reached.")
    {
    }
}
