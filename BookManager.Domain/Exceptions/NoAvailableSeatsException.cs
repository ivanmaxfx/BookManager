namespace BookManager.Domain.Exceptions
{
    public class NoAvailableSeatsException : Exception
    {
        public NoAvailableSeatsException()
            : base("No available seats for this event")
        {
        }

        public NoAvailableSeatsException(string message)
            : base(message)
        {
        }
    }
}
