using System.Collections.Concurrent;
using MyWebApiProject.Dtos;
using MyWebApiProject.Exceptions;
using MyWebApiProject.Models;

namespace MyWebApiProject.Services
{
    /// <summary>
    /// Потокобезопасный сервис для хранения мероприятий в памяти приложения.
    /// </summary>
    public class EventService : IEventService
    {
        private readonly ConcurrentDictionary<Guid, Event> _events = new();

        public PaginatedResult<Event> GetAll(EventQueryParameters queryParameters)
        {
            if (queryParameters.Page <= 0)
            {
                throw new ValidationException("Page must be greater than 0.");
            }

            if (queryParameters.PageSize <= 0)
            {
                throw new ValidationException("PageSize must be greater than 0.");
            }

            if (queryParameters.From.HasValue && queryParameters.To.HasValue &&
                queryParameters.From > queryParameters.To)
            {
                throw new ValidationException("'From' must be earlier than or equal to 'To'.");
            }

            var query = _events.Values.AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryParameters.Title))
            {
                query = query.Where(e =>
                    e.Title.Contains(queryParameters.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (queryParameters.From.HasValue)
            {
                query = query.Where(e => e.StartAt >= queryParameters.From.Value);
            }

            if (queryParameters.To.HasValue)
            {
                query = query.Where(e => e.EndAt <= queryParameters.To.Value);
            }

            query = query.OrderBy(e => e.StartAt);

            var totalCount = query.Count();

            var items = query
                .Skip((queryParameters.Page - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToList();

            return new PaginatedResult<Event>
            {
                TotalCount = totalCount,
                Page = queryParameters.Page,
                PageSize = items.Count,
                Items = items
            };
        }

        public Event GetById(Guid id)
        {
            if (!_events.TryGetValue(id, out var eventItem))
            {
                throw new NotFoundException($"Event with id '{id}' was not found.");
            }

            return eventItem;
        }

        public Event Create(Event newEvent)
        {
            ValidateEvent(newEvent);

            if (!_events.TryAdd(newEvent.Id, newEvent))
            {
                throw new ValidationException($"Event with id '{newEvent.Id}' already exists.");
            }

            return newEvent;
        }

        public void Update(Guid id, Event updatedEvent)
        {
            ValidateEvent(updatedEvent);

            while (true)
            {
                if (!_events.TryGetValue(id, out var existingEvent))
                {
                    throw new NotFoundException($"Event with id '{id}' was not found.");
                }

                updatedEvent.Id = id;

                if (_events.TryUpdate(id, updatedEvent, existingEvent))
                {
                    return;
                }
            }
        }

        public void Delete(Guid id)
        {
            if (!_events.TryRemove(id, out _))
            {
                throw new NotFoundException($"Event with id '{id}' was not found.");
            }
        }

        private static void ValidateEvent(Event eventItem)
        {
            if (string.IsNullOrWhiteSpace(eventItem.Title))
            {
                throw new ValidationException("Title is required.");
            }

            if (eventItem.EndAt <= eventItem.StartAt)
            {
                throw new ValidationException("EndAt must be later than StartAt.");
            }
        }
    }
}