using BookManager.Domain.Entities;
using BookManager.Domain.Enums;

namespace EventService.Tests
{
    public class BookingTests
    {
        [Fact]
        public void CreatePending_CreatesPendingBooking()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var beforeCreation = DateTime.UtcNow;

            // Act
            var booking = Booking.CreatePending(
                eventId,
                userId);

            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.NotEqual(Guid.Empty, booking.Id);
            Assert.Equal(eventId, booking.EventId);
            Assert.Equal(userId, booking.UserId);
            Assert.Equal(
                BookingStatus.Pending,
                booking.Status);
            Assert.Null(booking.ProcessedAt);

            Assert.InRange(
                booking.CreatedAt,
                beforeCreation,
                afterCreation);
        }

        [Fact]
        public void Confirm_ChangesStatusAndSetsProcessedAt()
        {
            // Arrange
            var booking = Booking.CreatePending(
                Guid.NewGuid(),
                Guid.NewGuid());

            // Act
            booking.Confirm();

            // Assert
            Assert.Equal(
                BookingStatus.Confirmed,
                booking.Status);

            Assert.NotNull(booking.ProcessedAt);
        }

        [Fact]
        public void Reject_ChangesStatusAndSetsProcessedAt()
        {
            // Arrange
            var booking = Booking.CreatePending(
                Guid.NewGuid(),
                Guid.NewGuid());

            // Act
            booking.Reject();

            // Assert
            Assert.Equal(
                BookingStatus.Rejected,
                booking.Status);

            Assert.NotNull(booking.ProcessedAt);
        }

        [Fact]
        public void Confirm_AlreadyProcessedBooking_ThrowsInvalidOperationException()
        {
            // Arrange
            var booking = Booking.CreatePending(
                Guid.NewGuid(),
                Guid.NewGuid());

            booking.Confirm();

            // Act
            var exception =
                Assert.Throws<InvalidOperationException>(
                    booking.Confirm);

            // Assert
            Assert.Equal(
                "Only a pending booking can be processed.",
                exception.Message);
        }

        [Fact]
        public void Cancel_ChangesStatusToCancelled()
        {
            // Arrange
            var booking = Booking.CreatePending(
                Guid.NewGuid(),
                Guid.NewGuid());

            // Act
            booking.Cancel();

            // Assert
            Assert.Equal(
                BookingStatus.Cancelled,
                booking.Status);

            Assert.NotNull(booking.ProcessedAt);
        }
    }
}
