using BookManager.Domain.Entities;
using BookManager.Domain.Enums;

namespace BookManager.Application.Dtos
{
    public class BookingInfo
    {
        public Guid Id { get; init; }

        public Guid EventId { get; init; }

        public Guid UserId { get; init; }

        public BookingStatus Status { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime? ProcessedAt { get; init; }

        public static BookingInfo FromBooking(
            Booking booking)
        {
            ArgumentNullException.ThrowIfNull(booking);

            return new BookingInfo
            {
                Id = booking.Id,
                EventId = booking.EventId,
                UserId = booking.UserId,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt,
                ProcessedAt = booking.ProcessedAt
            };
        }
    }
}
