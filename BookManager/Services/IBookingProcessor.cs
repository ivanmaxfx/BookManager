namespace MyWebApiProject.Services
{
    /// <summary>
    /// Обработчик ожидающих бронирований.
    /// </summary>
    public interface IBookingProcessor
    {
        Task ProcessPendingBookingsAsync(
            CancellationToken cancellationToken = default);
    }
}
