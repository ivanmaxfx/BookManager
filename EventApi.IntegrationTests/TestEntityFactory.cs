using BookManager.Domain.Entities;
using BookManager.Domain.Enums;
using EventEntity =
    BookManager.Domain.Entities.Event;

namespace EventApi.IntegrationTests
{
    internal static class TestEntityFactory
    {
        public static EventEntity CreateEvent(
            string title = "Integration event",
            DateTime? startAt = null,
            int totalSeats = 10)
        {
            var start =
                startAt ??
                DateTime.UtcNow.AddDays(1);

            return EventEntity.Create(
                title,
                "Integration test",
                start,
                start.AddHours(2),
                totalSeats);
        }

        public static User CreateUser(
            string? login = null,
            UserRole role = UserRole.User)
        {
            return User.Create(
                login ?? $"user-{Guid.NewGuid():N}",
                new string('A', 64),
                role);
        }

        public static Booking CreateBooking(
            Guid eventId,
            Guid userId)
        {
            return Booking.CreatePending(
                eventId,
                userId);
        }
    }
}
