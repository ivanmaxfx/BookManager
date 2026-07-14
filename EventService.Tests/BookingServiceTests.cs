using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using MyWebApiProject.Exceptions;
using MyWebApiProject.Models;
using MyWebApiProject.Services;

namespace EventService.Tests
{
    public class BookingServiceTests
    {
        [Fact]
        public async Task CreateBookingAsync_CreatesPendingBooking()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            Guid eventId;

            using (var scope = provider.CreateScope())
            {
                var eventService = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var created = await eventService.CreateAsync(
                    CreateEvent());

                eventId = created.Id;
            }

            using (var scope = provider.CreateScope())
            {
                var bookingService = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                var booking =
                    await bookingService.CreateBookingAsync(
                        eventId);

                Assert.NotEqual(Guid.Empty, booking.Id);
                Assert.Equal(eventId, booking.EventId);
                Assert.Equal(
                    BookingStatus.Pending,
                    booking.Status);
                Assert.Null(booking.ProcessedAt);
            }
        }

        [Fact]
        public async Task CreateBookingAsync_DecreasesAvailableSeats()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            Guid eventId;

            using (var scope = provider.CreateScope())
            {
                var eventService = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var created = await eventService.CreateAsync(
                    CreateEvent(totalSeats: 3));

                eventId = created.Id;
            }

            using (var scope = provider.CreateScope())
            {
                var bookingService = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                await bookingService.CreateBookingAsync(
                    eventId);
            }

            using (var scope = provider.CreateScope())
            {
                var eventService = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var eventItem =
                    await eventService.GetByIdAsync(
                        eventId);

                Assert.Equal(
                    2,
                    eventItem.AvailableSeats);
            }
        }

        [Fact]
        public async Task CreateBookingAsync_WhenFull_ThrowsNoAvailableSeatsException()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            Guid eventId;

            using (var scope = provider.CreateScope())
            {
                var eventService = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                eventId = (
                    await eventService.CreateAsync(
                        CreateEvent(totalSeats: 1)))
                    .Id;
            }

            using (var scope = provider.CreateScope())
            {
                var bookingService = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                await bookingService.CreateBookingAsync(
                    eventId);
            }

            using (var scope = provider.CreateScope())
            {
                var bookingService = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                var exception =
                    await Assert.ThrowsAsync<
                        NoAvailableSeatsException>(
                        () => bookingService
                            .CreateBookingAsync(
                                eventId));

                Assert.Equal(
                    "No available seats for this event",
                    exception.Message);
            }
        }

        [Fact]
        public async Task CreateBookingAsync_MissingEvent_ThrowsNotFoundException()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var bookingService = scope.ServiceProvider
                .GetRequiredService<IBookingService>();

            var eventId = Guid.NewGuid();

            await Assert.ThrowsAsync<
                NotFoundException>(
                () => bookingService.CreateBookingAsync(
                    eventId));
        }

        [Fact]
        public async Task ConfirmBookingAsync_SetsConfirmedStatusAndProcessedAt()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            var bookingId =
                await CreateBookingAsync(provider);

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                await service.ConfirmBookingAsync(
                    bookingId);
            }

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                var result =
                    await service.GetBookingByIdAsync(
                        bookingId);

                Assert.Equal(
                    BookingStatus.Confirmed,
                    result.Status);

                Assert.NotNull(result.ProcessedAt);
            }
        }

        [Fact]
        public async Task RejectBookingAsync_SetsRejectedStatusAndProcessedAt()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            var bookingId =
                await CreateBookingAsync(provider);

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                await service.RejectBookingAsync(
                    bookingId);
            }

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                var result =
                    await service.GetBookingByIdAsync(
                        bookingId);

                Assert.Equal(
                    BookingStatus.Rejected,
                    result.Status);

                Assert.NotNull(result.ProcessedAt);
            }
        }

        [Fact]
        public async Task GetPendingBookingsAsync_ReturnsOnlyPendingBookings()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            var firstBookingId =
                await CreateBookingAsync(provider);

            var secondBookingId =
                await CreateBookingAsync(provider);

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                await service.ConfirmBookingAsync(
                    firstBookingId);
            }

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                var result =
                    await service.GetPendingBookingsAsync();

                Assert.Single(result);
                Assert.Equal(
                    secondBookingId,
                    result.Single().Id);
            }
        }

        [Fact]
        public async Task ConcurrentBookings_DoNotExceedCapacity()
        {
            const int totalSeats = 5;
            const int requestCount = 20;

            var databaseName =
                Guid.NewGuid().ToString();

            using var provider =
                TestServiceProviderFactory.Create(
                    databaseName);

            Guid eventId;

            using (var scope = provider.CreateScope())
            {
                var eventService = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                eventId = (
                    await eventService.CreateAsync(
                        CreateEvent(totalSeats)))
                    .Id;
            }

            var successfulIds =
                new ConcurrentBag<Guid>();

            var noSeatsCount = 0;

            var unexpectedExceptions =
                new ConcurrentBag<Exception>();

            var tasks = Enumerable.Range(
                    0,
                    requestCount)
                .Select(_ => Task.Run(async () =>
                {
                    using var scope =
                        provider.CreateScope();

                    var bookingService =
                        scope.ServiceProvider
                            .GetRequiredService<
                                IBookingService>();

                    try
                    {
                        var booking =
                            await bookingService
                                .CreateBookingAsync(
                                    eventId);

                        successfulIds.Add(
                            booking.Id);
                    }
                    catch (
                        NoAvailableSeatsException)
                    {
                        Interlocked.Increment(
                            ref noSeatsCount);
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
                successfulIds.Count);

            Assert.Equal(
                requestCount - totalSeats,
                noSeatsCount);

            Assert.Equal(
                totalSeats,
                successfulIds.Distinct().Count());

            using (var scope = provider.CreateScope())
            {
                var eventService = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var eventItem =
                    await eventService.GetByIdAsync(
                        eventId);

                Assert.Equal(
                    0,
                    eventItem.AvailableSeats);
            }
        }

        [Fact]
        public async Task ConcurrentBookings_HaveUniqueIds()
        {
            const int totalSeats = 10;

            var databaseName =
                Guid.NewGuid().ToString();

            using var provider =
                TestServiceProviderFactory.Create(
                    databaseName);

            Guid eventId;

            using (var scope = provider.CreateScope())
            {
                var eventService = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                eventId = (
                    await eventService.CreateAsync(
                        CreateEvent(totalSeats)))
                    .Id;
            }

            var tasks = Enumerable.Range(
                    0,
                    totalSeats)
                .Select(_ => Task.Run(async () =>
                {
                    using var scope =
                        provider.CreateScope();

                    var service =
                        scope.ServiceProvider
                            .GetRequiredService<
                                IBookingService>();

                    return await service
                        .CreateBookingAsync(eventId);
                }))
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
        }

        private static async Task<Guid>
            CreateBookingAsync(
                ServiceProvider provider)
        {
            Guid eventId;

            using (var scope = provider.CreateScope())
            {
                var eventService = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                eventId = (
                    await eventService.CreateAsync(
                        CreateEvent()))
                    .Id;
            }

            using (var scope = provider.CreateScope())
            {
                var bookingService =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IBookingService>();

                return (
                    await bookingService
                        .CreateBookingAsync(eventId))
                    .Id;
            }
        }

        private static Event CreateEvent(
            int totalSeats = 10)
        {
            return Event.Create(
                "Booking test event",
                "Test description",
                new DateTime(
                    2026, 8, 10, 10, 0, 0),
                new DateTime(
                    2026, 8, 10, 12, 0, 0),
                totalSeats);
        }
    }
}
