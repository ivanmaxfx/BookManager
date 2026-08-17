using BookManager.Domain.Entities;
using BookManager.Domain.Enums;

namespace BookManager.Application.Dtos
{
    public class EventInfo
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public string? Description { get; init; }

        public DateTime StartAt { get; init; }

        public DateTime EndAt { get; init; }

        public int TotalSeats { get; init; }

        public int AvailableSeats { get; init; }

        public static EventInfo FromEvent(Event eventItem)
        {
            ArgumentNullException.ThrowIfNull(eventItem);

            return new EventInfo
            {
                Id = eventItem.Id,
                Title = eventItem.Title,
                Description = eventItem.Description,
                StartAt = eventItem.StartAt,
                EndAt = eventItem.EndAt,
                TotalSeats = eventItem.TotalSeats,
                AvailableSeats = eventItem.AvailableSeats
            };
        }
    }
}
