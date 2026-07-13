using MyWebApiProject.Models;

namespace MyWebApiProject.Dtos
{
    /// <summary>
    /// Информация о бронировании, возвращаемая клиенту.
    /// </summary>
    public class BookingInfo
    {
        public Guid Id { get; init; }

        public Guid EventId { get; init; }

        public BookingStatus Status { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime? ProcessedAt { get; init; }

        public static BookingInfo FromBooking(Booking booking)
        {
            ArgumentNullException.ThrowIfNull(booking);

            return new BookingInfo
            {
                Id = booking.Id,
                EventId = booking.EventId,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt,
                ProcessedAt = booking.ProcessedAt
            };
        }
    }
}
