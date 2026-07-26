using BookManager.Domain.Entities;
using BookManager.Domain.Enums;

namespace MyWebApiProject.DataAccess.Repositories
{
    public interface IEventRepository
    {
        Task<int> CountAsync(
            string? title,
            DateTime? from,
            DateTime? to,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Event>> GetPageAsync(
            string? title,
            DateTime? from,
            DateTime? to,
            int skip,
            int take,
            CancellationToken cancellationToken = default);

        Task<Event?> GetByIdAsync(
            Guid id,
            bool trackChanges,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Event eventItem,
            CancellationToken cancellationToken = default);

        void Update(Event eventItem);

        void Remove(Event eventItem);
    }
}
