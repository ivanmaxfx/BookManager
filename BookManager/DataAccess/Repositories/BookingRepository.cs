using Microsoft.EntityFrameworkCore;
using MyWebApiProject.Models;

namespace MyWebApiProject.DataAccess.Repositories
{
    public sealed class BookingRepository :
        IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Booking?> GetByIdAsync(
            Guid id,
            bool trackChanges,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Booking> query =
                _context.Bookings;

            if (!trackChanges)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(
                booking => booking.Id == id,
                cancellationToken);
        }

        public async Task<IReadOnlyCollection<Booking>>
            GetPendingAsync(
                CancellationToken cancellationToken = default)
        {
            return await _context.Bookings
                .AsNoTracking()
                .Where(booking =>
                    booking.Status ==
                    BookingStatus.Pending)
                .OrderBy(booking =>
                    booking.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<Guid>>
            GetPendingIdsAsync(
                CancellationToken cancellationToken = default)
        {
            return await _context.Bookings
                .AsNoTracking()
                .Where(booking =>
                    booking.Status ==
                    BookingStatus.Pending)
                .OrderBy(booking =>
                    booking.CreatedAt)
                .Select(booking => booking.Id)
                .ToListAsync(cancellationToken);
        }

        public Task AddAsync(
            Booking booking,
            CancellationToken cancellationToken = default)
        {
            return _context.Bookings
                .AddAsync(
                    booking,
                    cancellationToken)
                .AsTask();
        }

        public void Update(Booking booking)
        {
            _context.Bookings.Update(booking);
        }

        public void Remove(Booking booking)
        {
            _context.Bookings.Remove(booking);
        }
    }
}
