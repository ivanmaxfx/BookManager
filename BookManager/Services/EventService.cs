using Microsoft.EntityFrameworkCore;
using MyWebApiProject.DataAccess;
using MyWebApiProject.Dtos;
using MyWebApiProject.Exceptions;
using MyWebApiProject.Models;

namespace MyWebApiProject.Services
{
    public sealed class EventService : IEventService
    {
        private readonly AppDbContext _context;

        public EventService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<Event>> GetAllAsync(
            EventQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
        {
            ValidateQueryParameters(queryParameters);

            var query = _context.Events
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(
                    queryParameters.Title))
            {
                var normalizedTitle = queryParameters.Title
                    .Trim()
                    .ToLower();

                query = query.Where(eventItem =>
                    eventItem.Title
                        .ToLower()
                        .Contains(normalizedTitle));
            }

            if (queryParameters.From.HasValue)
            {
                query = query.Where(eventItem =>
                    eventItem.StartAt >=
                    queryParameters.From.Value);
            }

            if (queryParameters.To.HasValue)
            {
                query = query.Where(eventItem =>
                    eventItem.EndAt <=
                    queryParameters.To.Value);
            }

            query = query.OrderBy(
                eventItem => eventItem.StartAt);

            var totalCount = await query.CountAsync(
                cancellationToken);

            var items = await query
                .Skip(
                    (queryParameters.Page - 1) *
                    queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedResult<Event>
            {
                TotalCount = totalCount,
                Page = queryParameters.Page,
                PageSize = queryParameters.PageSize,
                Items = items
            };
        }

        public async Task<Event> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var eventItem = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == id,
                    cancellationToken);

            return eventItem
                ?? throw new NotFoundException(
                    $"Event with id '{id}' was not found.");
        }

        public async Task<Event> CreateAsync(
            Event newEvent,
            CancellationToken cancellationToken = default)
        {
            ValidateEvent(newEvent);

            newEvent.AvailableSeats =
                newEvent.TotalSeats;

            await _context.Events.AddAsync(
                newEvent,
                cancellationToken);

            try
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            catch (DbUpdateException)
            {
                throw new ValidationException(
                    $"Event with id '{newEvent.Id}' already exists.");
            }

            return newEvent;
        }

        public async Task UpdateAsync(
            Guid id,
            Event updatedEvent,
            CancellationToken cancellationToken = default)
        {
            ValidateEvent(updatedEvent);

            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(
                    item => item.Id == id,
                    cancellationToken);

            if (existingEvent is null)
            {
                throw new NotFoundException(
                    $"Event with id '{id}' was not found.");
            }

            var reservedSeats =
                existingEvent.TotalSeats -
                existingEvent.AvailableSeats;

            if (updatedEvent.TotalSeats < reservedSeats)
            {
                throw new ValidationException(
                    "TotalSeats cannot be less than reserved seats.");
            }

            existingEvent.Title =
                updatedEvent.Title.Trim();

            existingEvent.Description =
                updatedEvent.Description;

            existingEvent.StartAt =
                updatedEvent.StartAt;

            existingEvent.EndAt =
                updatedEvent.EndAt;

            existingEvent.TotalSeats =
                updatedEvent.TotalSeats;

            existingEvent.AvailableSeats =
                updatedEvent.TotalSeats - reservedSeats;

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(
                    item => item.Id == id,
                    cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(
                    $"Event with id '{id}' was not found.");
            }

            _context.Events.Remove(eventItem);

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        private static void ValidateQueryParameters(
            EventQueryParameters queryParameters)
        {
            if (queryParameters.Page <= 0)
            {
                throw new ValidationException(
                    "Page must be greater than 0.");
            }

            if (queryParameters.PageSize <= 0)
            {
                throw new ValidationException(
                    "PageSize must be greater than 0.");
            }

            if (queryParameters.From.HasValue &&
                queryParameters.To.HasValue &&
                queryParameters.From >
                queryParameters.To)
            {
                throw new ValidationException(
                    "'From' must be earlier than or equal to 'To'.");
            }
        }

        private static void ValidateEvent(Event eventItem)
        {
            if (string.IsNullOrWhiteSpace(
                    eventItem.Title))
            {
                throw new ValidationException(
                    "Title is required.");
            }

            if (eventItem.EndAt <= eventItem.StartAt)
            {
                throw new ValidationException(
                    "EndAt must be later than StartAt.");
            }

            if (eventItem.TotalSeats <= 0)
            {
                throw new ValidationException(
                    "TotalSeats must be greater than 0.");
            }
        }
    }
}
