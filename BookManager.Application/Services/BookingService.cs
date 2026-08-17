using BookManager.Application.Abstractions.Persistence;
using BookManager.Application.Dtos;
using BookManager.Domain.Exceptions;
using BookManager.Domain.Entities;

namespace BookManager.Application.Services
{
    public sealed class BookingService : IBookingService
    {
        private static readonly SemaphoreSlim BookingSemaphore =
            new(1, 1);

        private readonly IEventRepository _eventRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BookingService(
            IEventRepository eventRepository,
            IBookingRepository bookingRepository,
            IUnitOfWork unitOfWork)
        {
            _eventRepository = eventRepository;
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<BookingInfo> CreateBookingAsync(
            Guid eventId,
            CancellationToken cancellationToken = default)
        {
            await BookingSemaphore.WaitAsync(
                cancellationToken);

            try
            {
                var eventItem =
                    await _eventRepository.GetByIdAsync(
                        eventId,
                        trackChanges: true,
                        cancellationToken);

                if (eventItem is null)
                {
                    throw new NotFoundException(
                        $"Event with id '{eventId}' was not found.");
                }

                if (!eventItem.TryReserveSeats())
                {
                    throw new NoAvailableSeatsException();
                }

                var booking =
                    Booking.CreatePending(eventId);

                _eventRepository.Update(eventItem);

                await _bookingRepository.AddAsync(
                    booking,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return BookingInfo.FromBooking(
                    booking);
            }
            finally
            {
                BookingSemaphore.Release();
            }
        }

        public async Task<BookingInfo> GetBookingByIdAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default)
        {
            var booking =
                await _bookingRepository.GetByIdAsync(
                    bookingId,
                    trackChanges: false,
                    cancellationToken);

            if (booking is null)
            {
                throw new NotFoundException(
                    $"Booking with id '{bookingId}' was not found.");
            }

            return BookingInfo.FromBooking(
                booking);
        }

        public async Task<IReadOnlyCollection<BookingInfo>>
            GetPendingBookingsAsync(
                CancellationToken cancellationToken = default)
        {
            var bookings =
                await _bookingRepository.GetPendingAsync(
                    cancellationToken);

            return bookings
                .Select(BookingInfo.FromBooking)
                .ToList();
        }

        public async Task ConfirmBookingAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default)
        {
            var booking =
                await GetTrackedBookingOrThrowAsync(
                    bookingId,
                    cancellationToken);

            booking.Confirm();
            _bookingRepository.Update(booking);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task RejectBookingAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default)
        {
            var booking =
                await GetTrackedBookingOrThrowAsync(
                    bookingId,
                    cancellationToken);

            booking.Reject();
            _bookingRepository.Update(booking);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        private async Task<Booking>
            GetTrackedBookingOrThrowAsync(
                Guid bookingId,
                CancellationToken cancellationToken)
        {
            var booking =
                await _bookingRepository.GetByIdAsync(
                    bookingId,
                    trackChanges: true,
                    cancellationToken);

            return booking
                ?? throw new NotFoundException(
                    $"Booking with id '{bookingId}' was not found.");
        }
    }
}
