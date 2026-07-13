using MyWebApiProject.DataAccess;
using MyWebApiProject.Exceptions;
using MyWebApiProject.Models;
using MyWebApiProject.Services;
using EventServiceImpl = MyWebApiProject.Services.EventService;

namespace EventService.Tests
{
    public class BookingServiceTests
    {
        private static (
            BookingService BookingService,
            EventServiceImpl EventService) CreateServices()
        {
            var eventService = new EventServiceImpl();
            var bookingStore = new InMemoryBookingStore();
            var bookingService = new BookingService(
                eventService,
                bookingStore);

            return (bookingService, eventService);
        }

        private static Event CreateEvent(
            EventServiceImpl eventService)
        {
            var eventItem = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Test event",
                Description = "Event for booking tests",
                StartAt = new DateTime(2026, 8, 10, 10, 0, 0),
                EndAt = new DateTime(2026, 8, 10, 12, 0, 0)
            };

            return eventService.Create(eventItem);
        }

        [Fact]
        public async Task CreateBookingAsync_ForExistingEvent_ReturnsPendingBooking()
        {
            // Arrange
            var (bookingService, eventService) = CreateServices();
            var eventItem = CreateEvent(eventService);
            var beforeCreation = DateTime.UtcNow;

            // Act
            var result = await bookingService.CreateBookingAsync(
                eventItem.Id);

            var afterCreation = DateTime.UtcNow;

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(eventItem.Id, result.EventId);
            Assert.Equal(BookingStatus.Pending, result.Status);
            Assert.Null(result.ProcessedAt);
            Assert.InRange(
                result.CreatedAt,
                beforeCreation,
                afterCreation);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookings_HaveUniqueIds()
        {
            // Arrange
            var (bookingService, eventService) = CreateServices();
            var eventItem = CreateEvent(eventService);

            // Act
            var firstBooking =
                await bookingService.CreateBookingAsync(eventItem.Id);

            var secondBooking =
                await bookingService.CreateBookingAsync(eventItem.Id);

            // Assert
            Assert.NotEqual(firstBooking.Id, secondBooking.Id);
            Assert.Equal(eventItem.Id, firstBooking.EventId);
            Assert.Equal(eventItem.Id, secondBooking.EventId);
            Assert.Equal(BookingStatus.Pending, firstBooking.Status);
            Assert.Equal(BookingStatus.Pending, secondBooking.Status);
        }

        [Fact]
        public async Task GetBookingByIdAsync_ExistingBooking_ReturnsBooking()
        {
            // Arrange
            var (bookingService, eventService) = CreateServices();
            var eventItem = CreateEvent(eventService);

            var createdBooking =
                await bookingService.CreateBookingAsync(eventItem.Id);

            // Act
            var result =
                await bookingService.GetBookingByIdAsync(
                    createdBooking.Id);

            // Assert
            Assert.Equal(createdBooking.Id, result.Id);
            Assert.Equal(createdBooking.EventId, result.EventId);
            Assert.Equal(createdBooking.Status, result.Status);
            Assert.Equal(createdBooking.CreatedAt, result.CreatedAt);
            Assert.Equal(createdBooking.ProcessedAt, result.ProcessedAt);
        }

        [Fact]
        public async Task ConfirmBookingAsync_ChangesStatusToConfirmed()
        {
            // Arrange
            var (bookingService, eventService) = CreateServices();
            var eventItem = CreateEvent(eventService);

            var createdBooking =
                await bookingService.CreateBookingAsync(eventItem.Id);

            // Act
            await bookingService.ConfirmBookingAsync(
                createdBooking.Id);

            var result =
                await bookingService.GetBookingByIdAsync(
                    createdBooking.Id);

            // Assert
            Assert.Equal(BookingStatus.Confirmed, result.Status);
            Assert.NotNull(result.ProcessedAt);
        }

        [Fact]
        public async Task RejectBookingAsync_ChangesStatusToRejected()
        {
            // Arrange
            var (bookingService, eventService) = CreateServices();
            var eventItem = CreateEvent(eventService);

            var createdBooking =
                await bookingService.CreateBookingAsync(eventItem.Id);

            // Act
            await bookingService.RejectBookingAsync(
                createdBooking.Id);

            var result =
                await bookingService.GetBookingByIdAsync(
                    createdBooking.Id);

            // Assert
            Assert.Equal(BookingStatus.Rejected, result.Status);
            Assert.NotNull(result.ProcessedAt);
        }

        [Fact]
        public async Task CreateBookingAsync_NonExistingEvent_ThrowsNotFoundException()
        {
            // Arrange
            var (bookingService, _) = CreateServices();
            var eventId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => bookingService.CreateBookingAsync(eventId));

            // Assert
            Assert.Equal(
                $"Event with id '{eventId}' was not found.",
                exception.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_DeletedEvent_ThrowsNotFoundException()
        {
            // Arrange
            var (bookingService, eventService) = CreateServices();
            var eventItem = CreateEvent(eventService);

            eventService.Delete(eventItem.Id);

            // Act
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => bookingService.CreateBookingAsync(eventItem.Id));

            // Assert
            Assert.Equal(
                $"Event with id '{eventItem.Id}' was not found.",
                exception.Message);
        }

        [Fact]
        public async Task GetBookingByIdAsync_NonExistingBooking_ThrowsNotFoundException()
        {
            // Arrange
            var (bookingService, _) = CreateServices();
            var bookingId = Guid.NewGuid();

            // Act
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => bookingService.GetBookingByIdAsync(bookingId));

            // Assert
            Assert.Equal(
                $"Booking with id '{bookingId}' was not found.",
                exception.Message);
        }
    }
}
