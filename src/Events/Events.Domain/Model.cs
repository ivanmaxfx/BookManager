namespace Events.Domain;

public sealed class Event
{
    private Event()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } =
        string.Empty;

    public string? Description { get; private set; }

    public DateTime StartAt { get; private set; }

    public DateTime EndAt { get; private set; }

    public int TotalSeats { get; private set; }

    public int AvailableSeats { get; private set; }

    public static Event Create(
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats)
    {
        Validate(
            title,
            startAt,
            endAt,
            totalSeats);

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description,
            StartAt = NormalizeUtc(startAt),
            EndAt = NormalizeUtc(endAt),
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }

    public void Update(
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats)
    {
        Validate(
            title,
            startAt,
            endAt,
            totalSeats);

        var reserved =
            TotalSeats - AvailableSeats;

        if (totalSeats < reserved)
        {
            throw new DomainValidationException(
                "TotalSeats cannot be less than reserved seats.");
        }

        Title = title.Trim();
        Description = description;
        StartAt = NormalizeUtc(startAt);
        EndAt = NormalizeUtc(endAt);
        TotalSeats = totalSeats;
        AvailableSeats =
            totalSeats - reserved;
    }

    public bool TryReserveSeats(
        int count = 1)
    {
        if (count <= 0)
        {
            throw new DomainValidationException(
                "Seat count must be greater than zero.");
        }

        if (AvailableSeats < count)
        {
            return false;
        }

        AvailableSeats -= count;

        return true;
    }

    private static DateTime NormalizeUtc(
        DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            DateTimeKind.Unspecified =>
                DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc),

            _ => value
        };
    }

    private static void Validate(
        string title,
        DateTime startAt,
        DateTime endAt,
        int totalSeats)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException(
                "Title is required.");
        }

        if (endAt <= startAt)
        {
            throw new DomainValidationException(
                "EndAt must be later than StartAt.");
        }

        if (totalSeats <= 0)
        {
            throw new DomainValidationException(
                "TotalSeats must be greater than zero.");
        }
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
