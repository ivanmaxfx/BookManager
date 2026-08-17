using BookManager.Infrastructure.DataAccess;
using BookManager.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using BookManager.Domain.Entities;
using BookManager.Domain.Enums;

namespace BookManager.Infrastructure.DataAccess.Repositories
{
    public sealed class EventRepository : IEventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<int> CountAsync(
            string? title,
            DateTime? from,
            DateTime? to,
            CancellationToken cancellationToken = default)
        {
            return BuildFilteredQuery(
                    title,
                    from,
                    to)
                .CountAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<Event>>
            GetPageAsync(
                string? title,
                DateTime? from,
                DateTime? to,
                int skip,
                int take,
                CancellationToken cancellationToken = default)
        {
            return await BuildFilteredQuery(
                    title,
                    from,
                    to)
                .OrderBy(eventItem => eventItem.StartAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<Event?> GetByIdAsync(
            Guid id,
            bool trackChanges,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Event> query = _context.Events;

            if (!trackChanges)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(
                eventItem => eventItem.Id == id,
                cancellationToken);
        }

        public Task<bool> ExistsAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return _context.Events
                .AsNoTracking()
                .AnyAsync(
                    eventItem => eventItem.Id == id,
                    cancellationToken);
        }

        public Task AddAsync(
            Event eventItem,
            CancellationToken cancellationToken = default)
        {
            return _context.Events
                .AddAsync(
                    eventItem,
                    cancellationToken)
                .AsTask();
        }

        public void Update(Event eventItem)
        {
            _context.Events.Update(eventItem);
        }

        public void Remove(Event eventItem)
        {
            _context.Events.Remove(eventItem);
        }

        private IQueryable<Event> BuildFilteredQuery(
            string? title,
            DateTime? from,
            DateTime? to)
        {
            var query = _context.Events
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
            {
                var normalizedTitle = title
                    .Trim()
                    .ToLower();

                query = query.Where(eventItem =>
                    eventItem.Title
                        .ToLower()
                        .Contains(normalizedTitle));
            }

            if (from.HasValue)
            {
                query = query.Where(eventItem =>
                    eventItem.StartAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(eventItem =>
                    eventItem.EndAt <= to.Value);
            }

            return query;
        }
    }
}
