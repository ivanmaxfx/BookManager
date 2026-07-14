using Microsoft.EntityFrameworkCore;
using MyWebApiProject.DataAccess;
using MyWebApiProject.Dtos;
using MyWebApiProject.Exceptions;
using MyWebApiProject.Models;

namespace MyWebApiProject.Services
{
    public sealed class BookingService : IBookingService
    {
        private static readonly SemaphoreSlim BookingSemaphore =
            new(1, 1);

        private readonly AppDbContext _context;

        public BookingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<BookingInfo> CreateBookingAsync(
            Guid eventId,
            CancellationToken cancellationToken = default)
        {
            await BookingSemaphore.WaitAsync(
                cancellationToken);

            try
            {
                var eventItem = await _context.Events
                    .FirstOrDefaultAsync(
                        item => item.Id == eventId,
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

                await _context.Bookings.AddAsync(
                    booking,
                    cancellationToken);

                // Сохраняет одновременно уменьшение числа мест
                // и новую бронь в одной транзакции SaveChanges.
                await _context.SaveChangesAsync(
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
            var booking = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == bookingId,
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
            var bookings = await _context.Bookings
                .AsNoTracking()
                .Where(booking =>
                    booking.Status ==
                    BookingStatus.Pending)
                .OrderBy(booking =>
                    booking.CreatedAt)
                .ToListAsync(cancellationToken);

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

            await _context.SaveChangesAsync(
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

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        private async Task<Booking>
            GetTrackedBookingOrThrowAsync(
                Guid bookingId,
                CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(
                    item => item.Id == bookingId,
                    cancellationToken);

            return booking
                ?? throw new NotFoundException(
                    $"Booking with id '{bookingId}' was not found.");
        }
    }
}
