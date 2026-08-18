namespace BookManager.Domain.Exceptions
{
    public sealed class BookingLimitExceededException : Exception
    {
        public BookingLimitExceededException(int limit)
            : base(
                $"The active booking limit of {limit} has been reached.")
        {
            Limit = limit;
        }

        public int Limit { get; }
    }
}
