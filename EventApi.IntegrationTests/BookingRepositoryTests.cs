using BookManager.Domain.Enums;
using BookManager.Infrastructure.DataAccess.Repositories;
using BookManager.Infrastructure.DataAccess.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace EventApi.IntegrationTests
{
    [Collection(TestCollections.PostgreSql)]
    public sealed class BookingRepositoryTests
    {
        private readonly PostgreSqlFixture _fixture;

        public BookingRepositoryTests(
            PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AddAndGet_PersistsUserId()
        {
            await _fixture.ResetDatabaseAsync();

            var eventItem =
                TestEntityFactory.CreateEvent();

            var user =
                TestEntityFactory.CreateUser();

            var booking =
                TestEntityFactory.CreateBooking(
                    eventItem.Id,
                    user.Id);

            await using (var context =
                _fixture.CreateContext())
            {
                var events =
                    new EventRepository(context);

                var users =
                    new UserRepository(context);

                var bookings =
                    new BookingRepository(context);

                var uow =
                    new UnitOfWork(context);

                await events.AddAsync(eventItem);
                await users.AddAsync(user);
                await bookings.AddAsync(booking);

                await uow.SaveChangesAsync();
            }

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new BookingRepository(context);

                var stored =
                    await repository.GetByIdAsync(
                        booking.Id,
                        false);

                Assert.NotNull(stored);
                Assert.Equal(user.Id, stored.UserId);
                Assert.Equal(
                    BookingStatus.Pending,
                    stored.Status);

                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(stored).State);
            }
        }

        [Fact]
        public async Task CountActive_CountsPendingAndConfirmed()
        {
            await _fixture.ResetDatabaseAsync();

            var eventItem =
                TestEntityFactory.CreateEvent(
                    totalSeats: 10);

            var user =
                TestEntityFactory.CreateUser();

            var pending =
                TestEntityFactory.CreateBooking(
                    eventItem.Id,
                    user.Id);

            var confirmed =
                TestEntityFactory.CreateBooking(
                    eventItem.Id,
                    user.Id);

            confirmed.Confirm();

            var cancelled =
                TestEntityFactory.CreateBooking(
                    eventItem.Id,
                    user.Id);

            cancelled.Cancel();

            await using var context =
                _fixture.CreateContext();

            var events =
                new EventRepository(context);

            var users =
                new UserRepository(context);

            var bookings =
                new BookingRepository(context);

            var uow =
                new UnitOfWork(context);

            await events.AddAsync(eventItem);
            await users.AddAsync(user);
            await bookings.AddAsync(pending);
            await bookings.AddAsync(confirmed);
            await bookings.AddAsync(cancelled);

            await uow.SaveChangesAsync();

            Assert.Equal(
                2,
                await bookings.CountActiveByUserIdAsync(
                    user.Id));
        }
    }
}
