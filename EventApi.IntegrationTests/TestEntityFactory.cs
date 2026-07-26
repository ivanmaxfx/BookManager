using BookManager.Domain.Entities;
using BookManager.Domain.Enums;
using EventEntity = BookManager.Domain.Entities.Event;

namespace EventApi.IntegrationTests
{
    internal static class TestEntityFactory
    {
        public static EventEntity CreateEvent(
            string title = "Integration event",
            DateTime? startAt = null,
            int totalSeats = 10)
        {
            var start = startAt ??
                new DateTime(
                    2026,
                    8,
                    10,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc);

            return EventEntity.Create(
                title,
                "Integration test",
                start,
                start.AddHours(2),
                totalSeats);
        }

        public static Booking CreateBooking(
            Guid eventId)
        {
            return Booking.CreatePending(eventId);
        }
    }
}
