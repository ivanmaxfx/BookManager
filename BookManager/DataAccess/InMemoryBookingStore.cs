using System.Collections.Concurrent;
using MyWebApiProject.Models;

namespace MyWebApiProject.DataAccess
{
    /// <summary>
    /// Потокобезопасное хранилище бронирований в памяти.
    /// </summary>
    public class InMemoryBookingStore : IBookingStore
    {
        private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();

        public void Add(Booking booking)
        {
            ArgumentNullException.ThrowIfNull(booking);

            if (!_bookings.TryAdd(booking.Id, booking))
            {
                throw new InvalidOperationException(
                    $"Booking with id '{booking.Id}' already exists.");
            }
        }

        public Booking? GetById(Guid bookingId)
        {
            return _bookings.TryGetValue(bookingId, out var booking)
                ? booking
                : null;
        }

        public IReadOnlyCollection<Booking> GetPending()
        {
            return _bookings.Values
                .Where(booking => booking.Status == BookingStatus.Pending)
                .OrderBy(booking => booking.CreatedAt)
                .ToList();
        }

        public void Update(Booking booking)
        {
            ArgumentNullException.ThrowIfNull(booking);

            while (true)
            {
                if (!_bookings.TryGetValue(booking.Id, out var currentBooking))
                {
                    throw new InvalidOperationException(
                        $"Booking with id '{booking.Id}' does not exist.");
                }

                if (_bookings.TryUpdate(
                    booking.Id,
                    booking,
                    currentBooking))
                {
                    return;
                }
            }
        }
    }
}
