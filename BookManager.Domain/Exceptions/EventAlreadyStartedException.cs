namespace BookManager.Domain.Exceptions
{
    public sealed class EventAlreadyStartedException : Exception
    {
        public EventAlreadyStartedException()
            : base("The event has already started.")
        {
        }
    }
}
