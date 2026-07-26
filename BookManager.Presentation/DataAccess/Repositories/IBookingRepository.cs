using BookManager.Domain.Entities;
using BookManager.Domain.Enums;

namespace MyWebApiProject.DataAccess.Repositories
{
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(
            Guid id,
            bool trackChanges,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Booking>>
            GetPendingAsync(
                CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Guid>>
            GetPendingIdsAsync(
                CancellationToken cancellationToken = default);

        Task AddAsync(
            Booking booking,
            CancellationToken cancellationToken = default);

        void Update(Booking booking);

        void Remove(Booking booking);
    }
}
