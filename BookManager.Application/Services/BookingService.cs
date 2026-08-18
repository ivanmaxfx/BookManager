using BookManager.Application.Abstractions.Persistence;
using BookManager.Application.Dtos;
using BookManager.Domain.Entities;
using BookManager.Domain.Enums;
using BookManager.Domain.Exceptions;

namespace BookManager.Application.Services
{
    public sealed class BookingService : IBookingService
    {
        public const int MaxActiveBookingsPerUser = 10;

        private static readonly SemaphoreSlim BookingSemaphore =
            new(1, 1);

        private readonly IEventRepository _events;
        private readonly IBookingRepository _bookings;
        private readonly IUnitOfWork _unitOfWork;

        public BookingService(
            IEventRepository events,
            IBookingRepository bookings,
            IUnitOfWork unitOfWork)
        {
            _events = events;
            _bookings = bookings;
            _unitOfWork = unitOfWork;
        }

        public async Task<BookingInfo> CreateBookingAsync(
            Guid eventId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                throw new ValidationException(
                    "UserId must not be empty.");
            }

            await BookingSemaphore.WaitAsync(cancellationToken);

            try
            {
                var eventItem =
                    await _events.GetByIdAsync(
                        eventId,
                        true,
                        cancellationToken);

                if (eventItem is null)
                {
                    throw new NotFoundException(
                        $"Event with id '{eventId}' was not found.");
                }

                if (eventItem.StartAt <= DateTime.UtcNow)
                {
                    throw new EventAlreadyStartedException();
                }

                var activeCount =
                    await _bookings.CountActiveByUserIdAsync(
                        userId,
                        cancellationToken);

                if (activeCount >= MaxActiveBookingsPerUser)
                {
                    throw new BookingLimitExceededException(
                        MaxActiveBookingsPerUser);
                }

                if (!eventItem.TryReserveSeats())
                {
                    throw new NoAvailableSeatsException();
                }

                var booking =
                    Booking.CreatePending(
                        eventId,
                        userId);

                _events.Update(eventItem);

                await _bookings.AddAsync(
                    booking,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return BookingInfo.FromBooking(booking);
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
                await _bookings.GetByIdAsync(
                    bookingId,
                    false,
                    cancellationToken);

            if (booking is null)
            {
                throw new NotFoundException(
                    $"Booking with id '{bookingId}' was not found.");
            }

            return BookingInfo.FromBooking(booking);
        }

        public async Task<IReadOnlyCollection<BookingInfo>>
            GetPendingBookingsAsync(
                CancellationToken cancellationToken = default)
        {
            var bookings =
                await _bookings.GetPendingAsync(
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
                await GetTrackedAsync(
                    bookingId,
                    cancellationToken);

            booking.Confirm();
            _bookings.Update(booking);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task RejectBookingAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default)
        {
            var booking =
                await GetTrackedAsync(
                    bookingId,
                    cancellationToken);

            booking.Reject();
            _bookings.Update(booking);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task CancelBookingAsync(
            Guid bookingId,
            Guid currentUserId,
            UserRole currentUserRole,
            CancellationToken cancellationToken = default)
        {
            await BookingSemaphore.WaitAsync(cancellationToken);

            try
            {
                var booking =
                    await GetTrackedAsync(
                        bookingId,
                        cancellationToken);

                if (currentUserRole != UserRole.Admin &&
                    booking.UserId != currentUserId)
                {
                    throw new ForbiddenOperationException(
                        "You can cancel only your own bookings.");
                }

                var eventItem =
                    await _events.GetByIdAsync(
                        booking.EventId,
                        true,
                        cancellationToken);

                booking.Cancel();
                _bookings.Update(booking);

                if (eventItem is not null)
                {
                    eventItem.ReleaseSeats();
                    _events.Update(eventItem);
                }

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
            finally
            {
                BookingSemaphore.Release();
            }
        }

        private async Task<Booking> GetTrackedAsync(
            Guid bookingId,
            CancellationToken cancellationToken)
        {
            return
                await _bookings.GetByIdAsync(
                    bookingId,
                    true,
                    cancellationToken)
                ?? throw new NotFoundException(
                    $"Booking with id '{bookingId}' was not found.");
        }
    }
}
