using MyWebApiProject.DataAccess;
using MyWebApiProject.Dtos;
using MyWebApiProject.Exceptions;
using MyWebApiProject.Models;

namespace MyWebApiProject.Services
{
    public class BookingService : IBookingService
    {
        private readonly IEventService _eventService;
        private readonly IBookingStore _bookingStore;
        private readonly object _bookingLock = new();

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

            lock (_bookingLock)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var eventItem = _eventService.GetById(eventId);

                if (!eventItem.TryReserveSeats())
                {
                    throw new NoAvailableSeatsException();
                }

                try
                {
                    _eventService.Update(eventItem.Id, eventItem);

                    var booking = Booking.CreatePending(eventId);

                    _bookingStore.Add(booking);

                    return Task.FromResult(
                        BookingInfo.FromBooking(booking));
                }
                catch
                {
                    eventItem.ReleaseSeats();
                    _eventService.Update(eventItem.Id, eventItem);

                    throw;
                }
            }
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

        public Task<IReadOnlyCollection<BookingInfo>>
            GetPendingBookingsAsync(
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = _bookingStore
                .GetPending()
                .Select(BookingInfo.FromBooking)
                .ToList();

            return Task.FromResult<
                IReadOnlyCollection<BookingInfo>>(result);
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
