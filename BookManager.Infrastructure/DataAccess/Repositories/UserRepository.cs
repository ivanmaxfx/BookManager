using BookManager.Application.Abstractions.Persistence;
using BookManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookManager.Infrastructure.DataAccess.Repositories
{
    public sealed class UserRepository :
        IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByLoginAsync(
            string login,
            bool trackChanges,
            CancellationToken cancellationToken = default)
        {
            IQueryable<User> query = _context.Users;

            if (!trackChanges)
            {
                query = query.AsNoTracking();
            }

            var normalized =
                login.Trim().ToLowerInvariant();

            return await query.FirstOrDefaultAsync(
                user => user.Login == normalized,
                cancellationToken);
        }

        public async Task<User?> GetByIdAsync(
            Guid id,
            bool trackChanges,
            CancellationToken cancellationToken = default)
        {
            IQueryable<User> query = _context.Users;

            if (!trackChanges)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
        }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            return _context.Users
                .AddAsync(
                    user,
                    cancellationToken)
                .AsTask();
        }
    }
}
