using BookManager.Domain.Exceptions;
using BookManager.Domain.Entities;
using BookManager.Domain.Enums;

namespace EventService.Tests
{
    public class EventSeatTests
    {
        [Fact]
        public void Create_SetsAvailableSeatsEqualToTotalSeats()
        {
            const int totalSeats = 5;

            var eventItem = CreateEvent(
                totalSeats);

            Assert.Equal(
                totalSeats,
                eventItem.TotalSeats);

            Assert.Equal(
                totalSeats,
                eventItem.AvailableSeats);
        }

        [Fact]
        public void Create_WithInvalidTotalSeats_ThrowsValidationException()
        {
            var exception =
                Assert.Throws<ValidationException>(
                    () => CreateEvent(0));

            Assert.Equal(
                "TotalSeats must be greater than 0.",
                exception.Message);
        }

        [Fact]
        public void TryReserveSeats_WhenSeatsAreAvailable_DecreasesAvailableSeats()
        {
            var eventItem = CreateEvent(
                totalSeats: 3);

            var reserved =
                eventItem.TryReserveSeats();

            Assert.True(reserved);
            Assert.Equal(2, eventItem.AvailableSeats);
        }

        [Fact]
        public void TryReserveSeats_WhenSeatsAreUnavailable_ReturnsFalse()
        {
            var eventItem = CreateEvent(
                totalSeats: 1);

            Assert.True(
                eventItem.TryReserveSeats());

            Assert.False(
                eventItem.TryReserveSeats());

            Assert.Equal(0, eventItem.AvailableSeats);
        }

        [Fact]
        public void ReleaseSeats_RestoresAvailableSeats()
        {
            var eventItem = CreateEvent(
                totalSeats: 1);

            eventItem.TryReserveSeats();
            eventItem.ReleaseSeats();

            Assert.Equal(1, eventItem.AvailableSeats);
        }

        private static Event CreateEvent(
            int totalSeats)
        {
            return Event.Create(
                "Test event",
                null,
                new DateTime(2026, 8, 10, 10, 0, 0),
                new DateTime(2026, 8, 10, 12, 0, 0),
                totalSeats);
        }
    }
}
