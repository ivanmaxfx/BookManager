using System.Collections.Concurrent;
using MyWebApiProject.DataAccess;
using MyWebApiProject.Exceptions;
using MyWebApiProject.Models;
using MyWebApiProject.Services;
using EventServiceImpl = MyWebApiProject.Services.EventService;

namespace EventService.Tests
{
    public class BookingServiceTests
    {
        private const int DefaultTotalSeats = 10;

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

        private static Event CreateTestEvent(
            EventServiceImpl eventService,
            int totalSeats = DefaultTotalSeats)
        {
            var eventItem = Event.Create(
                "Test event",
                "Event for booking tests",
                new DateTime(2026, 8, 10, 10, 0, 0),
                new DateTime(2026, 8, 10, 12, 0, 0),
                totalSeats);

            return eventService.Create(eventItem);
        }

        [Fact]
        public async Task CreateBookingAsync_ForExistingEvent_ReturnsPendingBooking()
        {
            var (bookingService, eventService) =
                CreateServices();

            var eventItem = CreateTestEvent(
                eventService);

            var booking =
                await bookingService.CreateBookingAsync(
                    eventItem.Id);

            Assert.NotEqual(Guid.Empty, booking.Id);
            Assert.Equal(eventItem.Id, booking.EventId);
            Assert.Equal(
                BookingStatus.Pending,
                booking.Status);
            Assert.Null(booking.ProcessedAt);
        }

        [Fact]
        public async Task CreateBookingAsync_DecreasesAvailableSeatsByOne()
        {
            const int totalSeats = 3;

            var (bookingService, eventService) =
                CreateServices();

            var eventItem = CreateTestEvent(
                eventService,
                totalSeats);

            await bookingService.CreateBookingAsync(
                eventItem.Id);

            var storedEvent = eventService.GetById(
                eventItem.Id);

            Assert.Equal(
                totalSeats - 1,
                storedEvent.AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_UpToLimit_AllBookingsAreSuccessful()
        {
            const int totalSeats = 3;

            var (bookingService, eventService) =
                CreateServices();

            var eventItem = CreateTestEvent(
                eventService,
                totalSeats);

            var bookings = new List<
                MyWebApiProject.Dtos.BookingInfo>();

            for (var index = 0;
                 index < totalSeats;
                 index++)
            {
                bookings.Add(
                    await bookingService.CreateBookingAsync(
                        eventItem.Id));
            }

            Assert.Equal(totalSeats, bookings.Count);

            Assert.Equal(
                totalSeats,
                bookings.Select(booking => booking.Id)
                    .Distinct()
                    .Count());

            Assert.Equal(
                0,
                eventService.GetById(eventItem.Id)
                    .AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenSeatsAreExhausted_ThrowsNoAvailableSeatsException()
        {
            var (bookingService, eventService) =
                CreateServices();

            var eventItem = CreateTestEvent(
                eventService,
                totalSeats: 1);

            await bookingService.CreateBookingAsync(
                eventItem.Id);

            var exception =
                await Assert.ThrowsAsync<
                    NoAvailableSeatsException>(
                    () => bookingService.CreateBookingAsync(
                        eventItem.Id));

            Assert.Equal(
                "No available seats for this event",
                exception.Message);

            Assert.Equal(
                0,
                eventService.GetById(eventItem.Id)
                    .AvailableSeats);
        }

        [Fact]
        public async Task GetBookingByIdAsync_ExistingBooking_ReturnsBooking()
        {
            var (bookingService, eventService) =
                CreateServices();

            var eventItem = CreateTestEvent(
                eventService);

            var createdBooking =
                await bookingService.CreateBookingAsync(
                    eventItem.Id);

            var result =
                await bookingService.GetBookingByIdAsync(
                    createdBooking.Id);

            Assert.Equal(createdBooking.Id, result.Id);
            Assert.Equal(
                createdBooking.EventId,
                result.EventId);
            Assert.Equal(
                createdBooking.Status,
                result.Status);
            Assert.Equal(
                createdBooking.CreatedAt,
                result.CreatedAt);
        }

        [Fact]
        public async Task ConfirmBookingAsync_ChangesStatusToConfirmed()
        {
            var (bookingService, eventService) =
                CreateServices();

            var eventItem = CreateTestEvent(
                eventService);

            var createdBooking =
                await bookingService.CreateBookingAsync(
                    eventItem.Id);

            await bookingService.ConfirmBookingAsync(
                createdBooking.Id);

            var result =
                await bookingService.GetBookingByIdAsync(
                    createdBooking.Id);

            Assert.Equal(
                BookingStatus.Confirmed,
                result.Status);

            Assert.NotNull(result.ProcessedAt);
        }

        [Fact]
        public async Task RejectAndReleaseSeats_RestoresAvailableSeats()
        {
            var (bookingService, eventService) =
                CreateServices();

            var eventItem = CreateTestEvent(
                eventService,
                totalSeats: 1);

            var booking =
                await bookingService.CreateBookingAsync(
                    eventItem.Id);

            Assert.Equal(
                0,
                eventService.GetById(eventItem.Id)
                    .AvailableSeats);

            await bookingService.RejectBookingAsync(
                booking.Id);

            var storedEvent = eventService.GetById(
                eventItem.Id);

            storedEvent.ReleaseSeats();

            eventService.Update(
                storedEvent.Id,
                storedEvent);

            var rejectedBooking =
                await bookingService.GetBookingByIdAsync(
                    booking.Id);

            Assert.Equal(
                BookingStatus.Rejected,
                rejectedBooking.Status);

            Assert.NotNull(
                rejectedBooking.ProcessedAt);

            Assert.Equal(
                1,
                eventService.GetById(eventItem.Id)
                    .AvailableSeats);
        }

        [Fact]
        public async Task RejectAndReleaseSeats_AllowsNewBooking()
        {
            var (bookingService, eventService) =
                CreateServices();

            var eventItem = CreateTestEvent(
                eventService,
                totalSeats: 1);

            var firstBooking =
                await bookingService.CreateBookingAsync(
                    eventItem.Id);

            await bookingService.RejectBookingAsync(
                firstBooking.Id);

            var storedEvent = eventService.GetById(
                eventItem.Id);

            storedEvent.ReleaseSeats();

            eventService.Update(
                storedEvent.Id,
                storedEvent);

            var secondBooking =
                await bookingService.CreateBookingAsync(
                    eventItem.Id);

            Assert.NotEqual(
                firstBooking.Id,
                secondBooking.Id);

            Assert.Equal(
                BookingStatus.Pending,
                secondBooking.Status);

            Assert.Equal(
                0,
                eventService.GetById(eventItem.Id)
                    .AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_NonExistingEvent_ThrowsNotFoundException()
        {
            var (bookingService, _) =
                CreateServices();

            var eventId = Guid.NewGuid();

            var exception =
                await Assert.ThrowsAsync<
                    NotFoundException>(
                    () => bookingService.CreateBookingAsync(
                        eventId));

            Assert.Equal(
                $"Event with id '{eventId}' was not found.",
                exception.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_DeletedEvent_ThrowsNotFoundException()
        {
            var (bookingService, eventService) =
                CreateServices();

            var eventItem = CreateTestEvent(
                eventService);

            eventService.Delete(eventItem.Id);

            await Assert.ThrowsAsync<
                NotFoundException>(
                () => bookingService.CreateBookingAsync(
                    eventItem.Id));
        }

        [Fact]
        public async Task GetBookingByIdAsync_NonExistingBooking_ThrowsNotFoundException()
        {
            var (bookingService, _) =
                CreateServices();

            var bookingId = Guid.NewGuid();

            var exception =
                await Assert.ThrowsAsync<
                    NotFoundException>(
                    () => bookingService.GetBookingByIdAsync(
                        bookingId));

            Assert.Equal(
                $"Booking with id '{bookingId}' was not found.",
                exception.Message);
        }

        [Fact]
        public async Task ConcurrentBookings_DoNotExceedAvailableSeats()
        {
            const int totalSeats = 5;
            const int requestCount = 20;

            var (bookingService, eventService) =
                CreateServices();

            var eventItem = CreateTestEvent(
                eventService,
                totalSeats);

            var successfulBookingIds =
                new ConcurrentBag<Guid>();

            var unexpectedExceptions =
                new ConcurrentBag<Exception>();

            var noAvailableSeatsCount = 0;

            var tasks = Enumerable
                .Range(0, requestCount)
                .Select(_ => Task.Run(async () =>
                {
                    try
                    {
                        var booking =
                            await bookingService
                                .CreateBookingAsync(
                                    eventItem.Id);

                        successfulBookingIds.Add(
                            booking.Id);
                    }
                    catch (NoAvailableSeatsException)
                    {
                        Interlocked.Increment(
                            ref noAvailableSeatsCount);
                    }
                    catch (Exception exception)
                    {
                        unexpectedExceptions.Add(
                            exception);
                    }
                }))
                .ToArray();

            await Task.WhenAll(tasks);

            Assert.Empty(unexpectedExceptions);

            Assert.Equal(
                totalSeats,
                successfulBookingIds.Count);

            Assert.Equal(
                requestCount - totalSeats,
                noAvailableSeatsCount);

            Assert.Equal(
                totalSeats,
                successfulBookingIds
                    .Distinct()
                    .Count());

            Assert.Equal(
                0,
                eventService.GetById(eventItem.Id)
                    .AvailableSeats);
        }

        [Fact]
        public async Task ConcurrentBookings_HaveUniqueIds()
        {
            const int totalSeats = 10;

            var (bookingService, eventService) =
                CreateServices();

            var eventItem = CreateTestEvent(
                eventService,
                totalSeats);

            var tasks = Enumerable
                .Range(0, totalSeats)
                .Select(_ => Task.Run(
                    () => bookingService
                        .CreateBookingAsync(
                            eventItem.Id)))
                .ToArray();

            var bookings = await Task.WhenAll(
                tasks);

            Assert.Equal(
                totalSeats,
                bookings.Length);

            Assert.Equal(
                totalSeats,
                bookings
                    .Select(booking => booking.Id)
                    .Distinct()
                    .Count());

            Assert.Equal(
                0,
                eventService.GetById(eventItem.Id)
                    .AvailableSeats);
        }
    }
}
