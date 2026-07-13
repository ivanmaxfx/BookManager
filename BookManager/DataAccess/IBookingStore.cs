using MyWebApiProject.Models;

namespace MyWebApiProject.DataAccess
{
    /// <summary>
    /// Хранилище бронирований.
    /// </summary>
    public interface IBookingStore
    {
        void Add(Booking booking);

        Booking? GetById(Guid bookingId);

        IReadOnlyCollection<Booking> GetPending();

        void Update(Booking booking);
    }
}
