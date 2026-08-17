namespace BookManager.Domain.Exceptions
{
    /// <summary>
    /// Исключение, возникающее, когда ресурс не найден.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }
}