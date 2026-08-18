using BookManager.Domain.Exceptions;

namespace BookManager.Domain.Entities
{
    public class Event
    {
        private Event()
        {
        }

        public Guid Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public int TotalSeats { get; set; }

        public int AvailableSeats { get; set; }

        public ICollection<Booking> Bookings { get; private set; } =
            new List<Booking>();

        public static Event Create(
            string title,
            string? description,
            DateTime startAt,
            DateTime endAt,
            int totalSeats)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ValidationException(
                    "Title is required.");
            }

            if (endAt <= startAt)
            {
                throw new ValidationException(
                    "EndAt must be later than StartAt.");
            }

            if (totalSeats <= 0)
            {
                throw new ValidationException(
                    "TotalSeats must be greater than 0.");
            }

            return new Event
            {
                Id = Guid.NewGuid(),
                Title = title.Trim(),
                Description = description,
                StartAt = startAt,
                EndAt = endAt,
                TotalSeats = totalSeats,
                AvailableSeats = totalSeats
            };
        }

        public bool TryReserveSeats(int count = 1)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    "Seat count must be greater than 0.");
            }

            if (AvailableSeats < count)
            {
                return false;
            }

            AvailableSeats -= count;

            return true;
        }

        public void ReleaseSeats(int count = 1)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    "Seat count must be greater than 0.");
            }

            AvailableSeats = Math.Min(
                TotalSeats,
                AvailableSeats + count);
        }
    }
}
