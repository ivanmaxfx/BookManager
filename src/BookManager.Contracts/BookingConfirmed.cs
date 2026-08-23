namespace BookManager.Contracts;

public static class KafkaTopics
{
    public const string BookingConfirmed =
        "booking-confirmed";
}

public sealed record BookingConfirmed(
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatCount,
    DateTime ConfirmedAt);
