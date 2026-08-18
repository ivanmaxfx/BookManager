using BookManager.Domain.Entities;

namespace BookManager.Application.Abstractions.Persistence
{
    public interface IUserRepository
    {
        Task<User?> GetByLoginAsync(
            string login,
            bool trackChanges,
            CancellationToken cancellationToken = default);

        Task<User?> GetByIdAsync(
            Guid id,
            bool trackChanges,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            User user,
            CancellationToken cancellationToken = default);
    }
}
