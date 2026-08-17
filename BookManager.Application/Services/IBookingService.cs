using BookManager.Application.Dtos;

namespace BookManager.Application.Services
{
    /// <summary>
    /// Сервис для работы с бронированиями.
    /// </summary>
    public interface IBookingService
    {
        Task<BookingInfo> CreateBookingAsync(
            Guid eventId,
            CancellationToken cancellationToken = default);

        Task<BookingInfo> GetBookingByIdAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<BookingInfo>> GetPendingBookingsAsync(
            CancellationToken cancellationToken = default);

        Task ConfirmBookingAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default);

        Task RejectBookingAsync(
            Guid bookingId,
            CancellationToken cancellationToken = default);
    }
}
