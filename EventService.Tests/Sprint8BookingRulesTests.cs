using BookManager.Application.Services;
using BookManager.Domain.Entities;
using BookManager.Domain.Enums;
using BookManager.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Tests
{
    public sealed class Sprint8BookingRulesTests
    {
        [Fact]
        public async Task PastEvent_CannotBeBooked()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            var eventId =
                await CreateEventAsync(
                    provider,
                    DateTime.UtcNow.AddHours(-2),
                    20);

            using var scope = provider.CreateScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            await Assert.ThrowsAsync<
                EventAlreadyStartedException>(
                () => service.CreateBookingAsync(
                    eventId,
                    Guid.NewGuid()));
        }

        [Fact]
        public async Task EleventhActiveBooking_IsRejected()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            var eventId =
                await CreateEventAsync(
                    provider,
                    DateTime.UtcNow.AddDays(1),
                    20);

            var userId = Guid.NewGuid();

            using var scope = provider.CreateScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            for (var i = 0; i < 10; i++)
            {
                await service.CreateBookingAsync(
                    eventId,
                    userId);
            }

            var exception =
                await Assert.ThrowsAsync<
                    BookingLimitExceededException>(
                    () => service.CreateBookingAsync(
                        eventId,
                        userId));

            Assert.Equal(10, exception.Limit);
            Assert.Contains("10", exception.Message);
        }

        [Fact]
        public async Task DifferentUsers_HaveIndependentLimits()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            var eventId =
                await CreateEventAsync(
                    provider,
                    DateTime.UtcNow.AddDays(1),
                    25);

            var first = Guid.NewGuid();
            var second = Guid.NewGuid();

            using var scope = provider.CreateScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            for (var i = 0; i < 10; i++)
            {
                await service.CreateBookingAsync(
                    eventId,
                    first);
            }

            for (var i = 0; i < 10; i++)
            {
                await service.CreateBookingAsync(
                    eventId,
                    second);
            }

            await Assert.ThrowsAsync<
                BookingLimitExceededException>(
                () => service.CreateBookingAsync(
                    eventId,
                    first));
        }

        [Fact]
        public async Task User_CannotCancelAnotherUsersBooking()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            var eventId =
                await CreateEventAsync(
                    provider,
                    DateTime.UtcNow.AddDays(1),
                    10);

            var ownerId = Guid.NewGuid();

            Guid bookingId;

            using (var scope = provider.CreateScope())
            {
                var service =
                    scope.ServiceProvider
                        .GetRequiredService<IBookingService>();

                bookingId =
                    (
                        await service.CreateBookingAsync(
                            eventId,
                            ownerId)
                    ).Id;
            }

            using var cancelScope =
                provider.CreateScope();

            var cancelService =
                cancelScope.ServiceProvider
                    .GetRequiredService<IBookingService>();

            await Assert.ThrowsAsync<
                ForbiddenOperationException>(
                () => cancelService.CancelBookingAsync(
                    bookingId,
                    Guid.NewGuid(),
                    UserRole.User));
        }

        [Fact]
        public async Task Admin_CanCancelAnyBooking()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            var eventId =
                await CreateEventAsync(
                    provider,
                    DateTime.UtcNow.AddDays(1),
                    10);

            Guid bookingId;

            using (var scope = provider.CreateScope())
            {
                var service =
                    scope.ServiceProvider
                        .GetRequiredService<IBookingService>();

                bookingId =
                    (
                        await service.CreateBookingAsync(
                            eventId,
                            Guid.NewGuid())
                    ).Id;
            }

            using (var scope = provider.CreateScope())
            {
                var service =
                    scope.ServiceProvider
                        .GetRequiredService<IBookingService>();

                await service.CancelBookingAsync(
                    bookingId,
                    Guid.NewGuid(),
                    UserRole.Admin);
            }

            using (var scope = provider.CreateScope())
            {
                var service =
                    scope.ServiceProvider
                        .GetRequiredService<IBookingService>();

                var result =
                    await service.GetBookingByIdAsync(
                        bookingId);

                Assert.Equal(
                    BookingStatus.Cancelled,
                    result.Status);
            }
        }

        private static async Task<Guid> CreateEventAsync(
            ServiceProvider provider,
            DateTime startAt,
            int seats)
        {
            using var scope = provider.CreateScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<IEventService>();

            var eventItem = Event.Create(
                "Sprint 8 test event",
                null,
                startAt,
                startAt.AddHours(2),
                seats);

            return (
                await service.CreateAsync(eventItem)
            ).Id;
        }
    }
}
