using BookManager.Infrastructure.DataAccess.Repositories;
using BookManager.Infrastructure.DataAccess.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using BookManager.Application.Abstractions.Persistence;
using EventEntity = BookManager.Domain.Entities.Event;

namespace EventApi.IntegrationTests
{
    [Collection(TestCollections.PostgreSql)]
    public sealed class EventRepositoryTests
    {
        private readonly PostgreSqlFixture _fixture;

        public EventRepositoryTests(
            PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddAndGetByIdAsync_PersistsEvent()
        {
            await _fixture.ResetDatabaseAsync();

            var eventItem =
                TestEntityFactory.CreateEvent();

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new EventRepository(context);

                var unitOfWork =
                    new UnitOfWork(context);

                await repository.AddAsync(eventItem);
                await unitOfWork.SaveChangesAsync();
            }

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new EventRepository(context);

                var stored =
                    await repository.GetByIdAsync(
                        eventItem.Id,
                        trackChanges: false);

                Assert.NotNull(stored);
                Assert.Equal(
                    eventItem.Id,
                    stored.Id);

                Assert.Equal(
                    "Integration event",
                    stored.Title);

                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(stored).State);
            }
        }

        [Fact]
        public async Task Update_PersistsChangedEvent()
        {
            await _fixture.ResetDatabaseAsync();

            var eventId =
                await SeedEventAsync(
                    TestEntityFactory.CreateEvent(
                        title: "Old title"));

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new EventRepository(context);

                var unitOfWork =
                    new UnitOfWork(context);

                var eventItem =
                    await repository.GetByIdAsync(
                        eventId,
                        trackChanges: true);

                Assert.NotNull(eventItem);

                eventItem.Title = "Updated title";
                eventItem.TotalSeats = 20;
                eventItem.AvailableSeats = 20;

                repository.Update(eventItem);

                await unitOfWork.SaveChangesAsync();
            }

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new EventRepository(context);

                var stored =
                    await repository.GetByIdAsync(
                        eventId,
                        trackChanges: false);

                Assert.NotNull(stored);
                Assert.Equal(
                    "Updated title",
                    stored.Title);

                Assert.Equal(20, stored.TotalSeats);
            }
        }

        [Fact]
        public async Task CountAndGetPageAsync_ApplyFiltersAndPagination()
        {
            await _fixture.ResetDatabaseAsync();

            var events = new[]
            {
                TestEntityFactory.CreateEvent(
                    "Team Alpha",
                    UtcDate(2026, 8, 1)),

                TestEntityFactory.CreateEvent(
                    "Team Beta",
                    UtcDate(2026, 8, 2)),

                TestEntityFactory.CreateEvent(
                    "Other event",
                    UtcDate(2026, 8, 3)),

                TestEntityFactory.CreateEvent(
                    "Team Old",
                    UtcDate(2026, 7, 1))
            };

            await using var context =
                _fixture.CreateContext();

            var repository =
                new EventRepository(context);

            var unitOfWork =
                new UnitOfWork(context);

            foreach (var eventItem in events)
            {
                await repository.AddAsync(eventItem);
            }

            await unitOfWork.SaveChangesAsync();

            var from = UtcDate(2026, 8, 1);
            var to = UtcDate(2026, 8, 31)
                .AddDays(1)
                .AddTicks(-1);

            var count =
                await repository.CountAsync(
                    "team",
                    from,
                    to);

            var page =
                await repository.GetPageAsync(
                    "team",
                    from,
                    to,
                    skip: 1,
                    take: 1);

            Assert.Equal(2, count);
            Assert.Single(page);
            Assert.Equal(
                "Team Beta",
                page.Single().Title);
        }

        [Fact]
        public async Task ExistsAsync_ReturnsCorrectResult()
        {
            await _fixture.ResetDatabaseAsync();

            var eventItem =
                TestEntityFactory.CreateEvent();

            await SeedEventAsync(eventItem);

            await using var context =
                _fixture.CreateContext();

            var repository =
                new EventRepository(context);

            Assert.True(
                await repository.ExistsAsync(
                    eventItem.Id));

            Assert.False(
                await repository.ExistsAsync(
                    Guid.NewGuid()));
        }

        [Fact]
        public async Task Remove_DeletesEvent()
        {
            await _fixture.ResetDatabaseAsync();

            var eventId =
                await SeedEventAsync(
                    TestEntityFactory.CreateEvent());

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new EventRepository(context);

                var unitOfWork =
                    new UnitOfWork(context);

                var eventItem =
                    await repository.GetByIdAsync(
                        eventId,
                        trackChanges: true);

                Assert.NotNull(eventItem);

                repository.Remove(eventItem);

                await unitOfWork.SaveChangesAsync();
            }

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new EventRepository(context);

                Assert.False(
                    await repository.ExistsAsync(
                        eventId));
            }
        }

        private async Task<Guid> SeedEventAsync(
            EventEntity eventItem)
        {
            await using var context =
                _fixture.CreateContext();

            var repository =
                new EventRepository(context);

            var unitOfWork =
                new UnitOfWork(context);

            await repository.AddAsync(eventItem);
            await unitOfWork.SaveChangesAsync();

            return eventItem.Id;
        }

        private static DateTime UtcDate(
            int year,
            int month,
            int day)
        {
            return new DateTime(
                year,
                month,
                day,
                10,
                0,
                0,
                DateTimeKind.Utc);
        }
    }
}
