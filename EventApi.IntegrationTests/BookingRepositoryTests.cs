using BookManager.Infrastructure.DataAccess.Repositories;
using BookManager.Infrastructure.DataAccess.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using BookManager.Application.Abstractions.Persistence;
using BookManager.Domain.Entities;
using BookManager.Domain.Enums;

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
        public async Task AddAndGetByIdAsync_PersistsBooking()
        {
            await _fixture.ResetDatabaseAsync();

            var eventItem =
                TestEntityFactory.CreateEvent();

            var booking =
                TestEntityFactory.CreateBooking(
                    eventItem.Id);

            await using (var context =
                _fixture.CreateContext())
            {
                var eventRepository =
                    new EventRepository(context);

                var bookingRepository =
                    new BookingRepository(context);

                var unitOfWork =
                    new UnitOfWork(context);

                await eventRepository.AddAsync(
                    eventItem);

                await bookingRepository.AddAsync(
                    booking);

                await unitOfWork.SaveChangesAsync();
            }

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new BookingRepository(context);

                var stored =
                    await repository.GetByIdAsync(
                        booking.Id,
                        trackChanges: false);

                Assert.NotNull(stored);

                Assert.Equal(
                    booking.Id,
                    stored.Id);

                Assert.Equal(
                    eventItem.Id,
                    stored.EventId);

                Assert.Equal(
                    BookingStatus.Pending,
                    stored.Status);

                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(stored).State);
            }
        }

        [Fact]
        public async Task GetPendingMethods_ReturnOnlyPendingBookings()
        {
            await _fixture.ResetDatabaseAsync();

            var eventItem =
                TestEntityFactory.CreateEvent();

            var firstPending =
                TestEntityFactory.CreateBooking(
                    eventItem.Id);

            var secondPending =
                TestEntityFactory.CreateBooking(
                    eventItem.Id);

            var confirmed =
                TestEntityFactory.CreateBooking(
                    eventItem.Id);

            confirmed.Confirm();

            await using var context =
                _fixture.CreateContext();

            var eventRepository =
                new EventRepository(context);

            var bookingRepository =
                new BookingRepository(context);

            var unitOfWork =
                new UnitOfWork(context);

            await eventRepository.AddAsync(eventItem);

            await bookingRepository.AddAsync(
                firstPending);

            await bookingRepository.AddAsync(
                secondPending);

            await bookingRepository.AddAsync(
                confirmed);

            await unitOfWork.SaveChangesAsync();

            var pending =
                await bookingRepository
                    .GetPendingAsync();

            var pendingIds =
                await bookingRepository
                    .GetPendingIdsAsync();

            Assert.Equal(2, pending.Count);
            Assert.Equal(2, pendingIds.Count);

            Assert.Contains(
                firstPending.Id,
                pendingIds);

            Assert.Contains(
                secondPending.Id,
                pendingIds);

            Assert.DoesNotContain(
                confirmed.Id,
                pendingIds);

            Assert.All(
                pending,
                booking => Assert.Equal(
                    BookingStatus.Pending,
                    booking.Status));
        }

        [Fact]
        public async Task Update_PersistsChangedStatus()
        {
            await _fixture.ResetDatabaseAsync();

            var bookingId =
                await SeedBookingAsync();

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new BookingRepository(context);

                var unitOfWork =
                    new UnitOfWork(context);

                var booking =
                    await repository.GetByIdAsync(
                        bookingId,
                        trackChanges: true);

                Assert.NotNull(booking);

                booking.Confirm();
                repository.Update(booking);

                await unitOfWork.SaveChangesAsync();
            }

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new BookingRepository(context);

                var stored =
                    await repository.GetByIdAsync(
                        bookingId,
                        trackChanges: false);

                Assert.NotNull(stored);

                Assert.Equal(
                    BookingStatus.Confirmed,
                    stored.Status);

                Assert.NotNull(stored.ProcessedAt);
            }
        }

        [Fact]
        public async Task Remove_DeletesBooking()
        {
            await _fixture.ResetDatabaseAsync();

            var bookingId =
                await SeedBookingAsync();

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new BookingRepository(context);

                var unitOfWork =
                    new UnitOfWork(context);

                var booking =
                    await repository.GetByIdAsync(
                        bookingId,
                        trackChanges: true);

                Assert.NotNull(booking);

                repository.Remove(booking);

                await unitOfWork.SaveChangesAsync();
            }

            await using (var context =
                _fixture.CreateContext())
            {
                var repository =
                    new BookingRepository(context);

                var stored =
                    await repository.GetByIdAsync(
                        bookingId,
                        trackChanges: false);

                Assert.Null(stored);
            }
        }

        private async Task<Guid> SeedBookingAsync()
        {
            var eventItem =
                TestEntityFactory.CreateEvent();

            var booking =
                TestEntityFactory.CreateBooking(
                    eventItem.Id);

            await using var context =
                _fixture.CreateContext();

            var eventRepository =
                new EventRepository(context);

            var bookingRepository =
                new BookingRepository(context);

            var unitOfWork =
                new UnitOfWork(context);

            await eventRepository.AddAsync(eventItem);
            await bookingRepository.AddAsync(booking);

            await unitOfWork.SaveChangesAsync();

            return booking.Id;
        }
    }
}
