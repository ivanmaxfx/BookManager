namespace MyWebApiProject.Models
{
    /// <summary>
    /// Бронирование мероприятия.
    /// </summary>
    public class Booking
    {
        private Booking()
        {
        }

        /// <summary>
        /// Уникальный идентификатор бронирования.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Идентификатор мероприятия.
        /// </summary>
        public Guid EventId { get; private set; }

        public Event Event { get; private set; } = null!;

        /// <summary>
        /// Текущий статус бронирования.
        /// </summary>
        public BookingStatus Status { get; private set; }

        /// <summary>
        /// Дата создания бронирования.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Дата завершения обработки.
        /// </summary>
        public DateTime? ProcessedAt { get; private set; }

        /// <summary>
        /// Создаёт новое бронирование в статусе Pending.
        /// </summary>
        public static Booking CreatePending(Guid eventId)
        {
            if (eventId == Guid.Empty)
            {
                throw new ArgumentException(
                    "EventId must not be empty.",
                    nameof(eventId));
            }

            return new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            };
        }

        /// <summary>
        /// Подтверждает бронирование.
        /// </summary>
        public void Confirm()
        {
            EnsurePending();

            Status = BookingStatus.Confirmed;
            ProcessedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Отклоняет бронирование.
        /// </summary>
        public void Reject()
        {
            EnsurePending();

            Status = BookingStatus.Rejected;
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
