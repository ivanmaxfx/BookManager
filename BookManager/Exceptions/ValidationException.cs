namespace MyWebApiProject.Exceptions
{
    /// <summary>
    /// Исключение для ошибок валидации бизнес-логики.
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message)
        {
        }
    }
}