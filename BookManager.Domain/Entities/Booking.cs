using BookManager.Domain.Enums;
using BookManager.Domain.Exceptions;

namespace BookManager.Domain.Entities
{
    public class Booking
    {
        private Booking()
        {
        }

        public Guid Id { get; private set; }

        public Guid EventId { get; private set; }

        public Event Event { get; private set; } = null!;

        public Guid UserId { get; private set; }

        public User User { get; private set; } = null!;

        public BookingStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime? ProcessedAt { get; private set; }

        public static Booking CreatePending(
            Guid eventId,
            Guid userId)
        {
            if (eventId == Guid.Empty)
            {
                throw new ArgumentException(
                    "EventId must not be empty.",
                    nameof(eventId));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "UserId must not be empty.",
                    nameof(userId));
            }

            return new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                UserId = userId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Confirm()
        {
            EnsurePending();

            Status = BookingStatus.Confirmed;
            ProcessedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            EnsurePending();

            Status = BookingStatus.Rejected;
            ProcessedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status == BookingStatus.Cancelled)
            {
                throw new ValidationException(
                    "Booking is already cancelled.");
            }

            if (Status == BookingStatus.Rejected)
            {
                throw new ValidationException(
                    "Rejected booking cannot be cancelled.");
            }

            Status = BookingStatus.Cancelled;
            ProcessedAt = DateTime.UtcNow;
        }

        private void EnsurePending()
        {
            if (Status != BookingStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Only a pending booking can be processed.");
            }
        }
    }
}
