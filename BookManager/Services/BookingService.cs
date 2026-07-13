using MyWebApiProject.DataAccess;
using MyWebApiProject.Dtos;
using MyWebApiProject.Exceptions;
using MyWebApiProject.Models;

namespace MyWebApiProject.Services
{
    /// <summary>
    /// Бизнес-логика работы с бронированиями.
    /// </summary>
    public class BookingService : IBookingService
    {
        private readonly IEventService _eventService;
        private readonly IBookingStore _bookingStore;

        public BookingService(
            IEventService eventService,
            IBookingStore bookingStore)
        {
            _eventService = eventService;
            _bookingStore = bookingStore;
        }

        public Task<BookingInfo> CreateBookingAsync(
            Guid eventId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Проверяет, что мероприятие существует.
            // При отсутствии EventService выбрасывает NotFoundException.
            _eventService.GetById(eventId);

            var booking = Booking.CreatePending(eventId);

            _bookingStore.Add(booking);

            return Task.FromResult(
                BookingInfo.FromBooking(booking));
        }

        public Task<BookingInfo> GetBookingByIdAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var booking = GetBookingOrThrow(bookingId);

            return Task.FromResult(
                BookingInfo.FromBooking(booking));
        }

        public Task<IReadOnlyCollection<BookingInfo>> GetPendingBookingsAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = _bookingStore
                .GetPending()
                .Select(BookingInfo.FromBooking)
                .ToList();

            return Task.FromResult<IReadOnlyCollection<BookingInfo>>(result);
        }

        public Task ConfirmBookingAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var booking = GetBookingOrThrow(bookingId);

            booking.Confirm();
            _bookingStore.Update(booking);

            return Task.CompletedTask;
        }

        public Task RejectBookingAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var booking = GetBookingOrThrow(bookingId);

            booking.Reject();
            _bookingStore.Update(booking);

            return Task.CompletedTask;
        }

        private Booking GetBookingOrThrow(Guid bookingId)
        {
            return _bookingStore.GetById(bookingId)
                ?? throw new NotFoundException(
                    $"Booking with id '{bookingId}' was not found.");
        }
    }
}
