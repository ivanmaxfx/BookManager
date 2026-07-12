namespace MyWebApiProject.Exceptions
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