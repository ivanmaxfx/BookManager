using Microsoft.Extensions.DependencyInjection;
using MyWebApiProject.Dtos;
using BookManager.Domain.Exceptions;
using BookManager.Domain.Entities;
using BookManager.Domain.Enums;
using MyWebApiProject.Services;

namespace EventService.Tests
{
    public class EventServiceTests
    {
        [Fact]
        public async Task CreateAsync_AddsEvent()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<IEventService>();

            var eventItem = CreateEvent(
                title: "Meeting");

            var created = await service.CreateAsync(
                eventItem);

            var stored = await service.GetByIdAsync(
                created.Id);

            Assert.Equal(created.Id, stored.Id);
            Assert.Equal("Meeting", stored.Title);
            Assert.Equal(10, stored.TotalSeats);
            Assert.Equal(10, stored.AvailableSeats);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingEvent_ReturnsEvent()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            Guid eventId;

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var created = await service.CreateAsync(
                    CreateEvent());

                eventId = created.Id;
            }

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var result = await service.GetByIdAsync(
                    eventId);

                Assert.Equal(eventId, result.Id);
            }
        }

        [Fact]
        public async Task GetByIdAsync_MissingEvent_ThrowsNotFoundException()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<IEventService>();

            var id = Guid.NewGuid();

            var exception =
                await Assert.ThrowsAsync<
                    NotFoundException>(
                    () => service.GetByIdAsync(id));

            Assert.Equal(
                $"Event with id '{id}' was not found.",
                exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ModifiesEvent()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            Guid eventId;

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var created = await service.CreateAsync(
                    CreateEvent(
                        title: "Old title",
                        totalSeats: 5));

                eventId = created.Id;
            }

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var updated = CreateEvent(
                    title: "New title",
                    totalSeats: 8);

                await service.UpdateAsync(
                    eventId,
                    updated);
            }

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var result = await service.GetByIdAsync(
                    eventId);

                Assert.Equal("New title", result.Title);
                Assert.Equal(8, result.TotalSeats);
                Assert.Equal(8, result.AvailableSeats);
            }
        }

        [Fact]
        public async Task UpdateAsync_TotalSeatsBelowReserved_ThrowsValidationException()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            Guid eventId;

            using (var scope = provider.CreateScope())
            {
                var eventService = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var bookingService = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                var created = await eventService.CreateAsync(
                    CreateEvent(totalSeats: 3));

                eventId = created.Id;

                await bookingService.CreateBookingAsync(
                    eventId);

                await bookingService.CreateBookingAsync(
                    eventId);
            }

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var updated = CreateEvent(
                    totalSeats: 1);

                var exception =
                    await Assert.ThrowsAsync<
                        ValidationException>(
                        () => service.UpdateAsync(
                            eventId,
                            updated));

                Assert.Equal(
                    "TotalSeats cannot be less than reserved seats.",
                    exception.Message);
            }
        }

        [Fact]
        public async Task DeleteAsync_RemovesEvent()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            Guid eventId;

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                var created = await service.CreateAsync(
                    CreateEvent());

                eventId = created.Id;

                await service.DeleteAsync(eventId);
            }

            using (var scope = provider.CreateScope())
            {
                var service = scope.ServiceProvider
                    .GetRequiredService<IEventService>();

                await Assert.ThrowsAsync<
                    NotFoundException>(
                    () => service.GetByIdAsync(eventId));
            }
        }

        [Fact]
        public async Task GetAllAsync_FiltersByTitle()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<IEventService>();

            await service.CreateAsync(
                CreateEvent(title: "Team meeting"));

            await service.CreateAsync(
                CreateEvent(title: "Workshop"));

            var result = await service.GetAllAsync(
                new EventQueryParameters
                {
                    Title = "meeting"
                });

            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(
                "Team meeting",
                result.Items.Single().Title);
        }

        [Fact]
        public async Task GetAllAsync_FiltersByDateRange()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<IEventService>();

            await service.CreateAsync(
                CreateEvent(
                    title: "Early event",
                    startAt: new DateTime(
                        2026, 5, 1, 10, 0, 0)));

            await service.CreateAsync(
                CreateEvent(
                    title: "Late event",
                    startAt: new DateTime(
                        2026, 5, 10, 10, 0, 0)));

            var result = await service.GetAllAsync(
                new EventQueryParameters
                {
                    From = new DateTime(2026, 5, 5),
                    To = new DateTime(
                        2026, 5, 15, 23, 59, 59)
                });

            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(
                "Late event",
                result.Items.Single().Title);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsRequestedPage()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<IEventService>();

            for (var index = 1; index <= 15; index++)
            {
                await service.CreateAsync(
                    CreateEvent(
                        title: $"Event {index}",
                        startAt: new DateTime(
                            2026, 5, index, 10, 0, 0)));
            }

            var result = await service.GetAllAsync(
                new EventQueryParameters
                {
                    Page = 2,
                    PageSize = 5
                });

            Assert.Equal(15, result.TotalCount);
            Assert.Equal(2, result.Page);
            Assert.Equal(5, result.PageSize);
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(
                "Event 6",
                result.Items.First().Title);
        }

        [Fact]
        public async Task GetAllAsync_AppliesCombinedFilters()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<IEventService>();

            await service.CreateAsync(
                CreateEvent(
                    title: "Team meeting",
                    startAt: new DateTime(
                        2026, 6, 1, 10, 0, 0)));

            await service.CreateAsync(
                CreateEvent(
                    title: "Team meeting old",
                    startAt: new DateTime(
                        2026, 4, 1, 10, 0, 0)));

            await service.CreateAsync(
                CreateEvent(
                    title: "Workshop",
                    startAt: new DateTime(
                        2026, 6, 2, 10, 0, 0)));

            var result = await service.GetAllAsync(
                new EventQueryParameters
                {
                    Title = "team",
                    From = new DateTime(2026, 5, 1),
                    To = new DateTime(
                        2026, 6, 30, 23, 59, 59)
                });

            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(
                "Team meeting",
                result.Items.Single().Title);
        }

        [Fact]
        public async Task GetAllAsync_InvalidPage_ThrowsValidationException()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<IEventService>();

            var exception =
                await Assert.ThrowsAsync<
                    ValidationException>(
                    () => service.GetAllAsync(
                        new EventQueryParameters
                        {
                            Page = 0,
                            PageSize = 10
                        }));

            Assert.Equal(
                "Page must be greater than 0.",
                exception.Message);
        }

        [Fact]
        public async Task GetAllAsync_InvalidPageSize_ThrowsValidationException()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<IEventService>();

            var exception =
                await Assert.ThrowsAsync<
                    ValidationException>(
                    () => service.GetAllAsync(
                        new EventQueryParameters
                        {
                            Page = 1,
                            PageSize = 0
                        }));

            Assert.Equal(
                "PageSize must be greater than 0.",
                exception.Message);
        }

        [Fact]
        public async Task GetAllAsync_FromAfterTo_ThrowsValidationException()
        {
            using var provider =
                TestServiceProviderFactory.Create();

            using var scope = provider.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<IEventService>();

            await Assert.ThrowsAsync<
                ValidationException>(
                () => service.GetAllAsync(
                    new EventQueryParameters
                    {
                        From = new DateTime(2026, 6, 1),
                        To = new DateTime(2026, 5, 1)
                    }));
        }

        private static Event CreateEvent(
            string title = "Test event",
            int totalSeats = 10,
            DateTime? startAt = null)
        {
            var start = startAt ??
                new DateTime(
                    2026, 8, 10, 10, 0, 0);

            return Event.Create(
                title,
                "Test description",
                start,
                start.AddHours(2),
                totalSeats);
        }
    }
}
