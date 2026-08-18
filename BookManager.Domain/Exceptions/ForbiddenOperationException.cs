namespace BookManager.Domain.Exceptions
{
    public sealed class ForbiddenOperationException : Exception
    {
        public ForbiddenOperationException(
            string message =
                "You do not have permission to perform this operation.")
            : base(message)
        {
        }
    }
}
