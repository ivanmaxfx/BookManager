using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyWebApiProject.DataAccess;
using MyWebApiProject.Models;
using MyWebApiProject.Options;
using MyWebApiProject.Services;
using EventServiceImpl = MyWebApiProject.Services.EventService;

namespace EventService.Tests
{
    public class BookingProcessorTests
    {
        [Fact]
        public async Task ProcessPendingBookingsAsync_ConfirmsPendingBooking()
        {
            // Arrange
            var eventService = new EventServiceImpl();
            var bookingStore = new InMemoryBookingStore();

            var bookingService = new BookingService(
                eventService,
                bookingStore);

            var eventItem = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Background processing test",
                StartAt = new DateTime(2026, 8, 10, 10, 0, 0),
                EndAt = new DateTime(2026, 8, 10, 12, 0, 0)
            };

            eventService.Create(eventItem);

            var createdBooking =
                await bookingService.CreateBookingAsync(eventItem.Id);

            var options = Options.Create(
                new BookingProcessingOptions
                {
                    PollIntervalMilliseconds = 1,
                    ProcessingDelayMilliseconds = 1
                });

            var processor = new BookingProcessor(
                bookingService,
                options,
                NullLogger<BookingProcessor>.Instance);

            // Act
            await processor.ProcessPendingBookingsAsync();

            var result =
                await bookingService.GetBookingByIdAsync(
                    createdBooking.Id);

            // Assert
            Assert.Equal(BookingStatus.Confirmed, result.Status);
            Assert.NotNull(result.ProcessedAt);
        }
    }
}
