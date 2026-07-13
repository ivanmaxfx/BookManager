using MyWebApiProject.DataAccess;
using MyWebApiProject.Models;

namespace EventService.Tests
{
    public class InMemoryBookingStoreTests
    {
        [Fact]
        public void AddAndGetById_ReturnsStoredBooking()
        {
            // Arrange
            var store = new InMemoryBookingStore();
            var booking = Booking.CreatePending(Guid.NewGuid());

            // Act
            store.Add(booking);
            var result = store.GetById(booking.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(booking.Id, result.Id);
            Assert.Equal(booking.EventId, result.EventId);
        }

        [Fact]
        public void GetById_NonExistingBooking_ReturnsNull()
        {
            // Arrange
            var store = new InMemoryBookingStore();

            // Act
            var result = store.GetById(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetPending_ReturnsOnlyPendingBookings()
        {
            // Arrange
            var store = new InMemoryBookingStore();

            var pendingBooking =
                Booking.CreatePending(Guid.NewGuid());

            var confirmedBooking =
                Booking.CreatePending(Guid.NewGuid());

            confirmedBooking.Confirm();

            store.Add(pendingBooking);
            store.Add(confirmedBooking);

            // Act
            var result = store.GetPending();

            // Assert
            Assert.Single(result);
            Assert.Equal(pendingBooking.Id, result.Single().Id);
            Assert.Equal(
                BookingStatus.Pending,
                result.Single().Status);
        }

        [Fact]
        public void Add_SameBookingTwice_ThrowsInvalidOperationException()
        {
            // Arrange
            var store = new InMemoryBookingStore();
            var booking = Booking.CreatePending(Guid.NewGuid());

            store.Add(booking);

            // Act
            var exception = Assert.Throws<InvalidOperationException>(
                () => store.Add(booking));

            // Assert
            Assert.Equal(
                $"Booking with id '{booking.Id}' already exists.",
                exception.Message);
        }
    }
}
