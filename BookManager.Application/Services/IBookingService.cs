using BookManager.Application.Dtos;
using BookManager.Domain.Enums;

namespace BookManager.Application.Services
{
    public interface IBookingService
    {
        Task<BookingInfo> CreateBookingAsync(
            Guid eventId,
            Guid userId,
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

        Task CancelBookingAsync(
            Guid bookingId,
            Guid currentUserId,
            UserRole currentUserRole,
            CancellationToken cancellationToken = default);
    }
}
