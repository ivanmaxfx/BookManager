using MyWebApiProject.DataAccess;
using MyWebApiProject.Dtos;
using MyWebApiProject.Exceptions;
using MyWebApiProject.Models;

namespace MyWebApiProject.Services
{
    public class EventService : IEventService
    {
        private readonly IEventStore _eventStore;

        public EventService()
            : this(new InMemoryEventStore())
        {
        }

        public EventService(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public PaginatedResult<Event> GetAll(
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
                queryParameters.From > queryParameters.To)
            {
                throw new ValidationException(
                    "'From' must be earlier than or equal to 'To'.");
            }

            var query = _eventStore
                .GetAll()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryParameters.Title))
            {
                query = query.Where(eventItem =>
                    eventItem.Title.Contains(
                        queryParameters.Title,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (queryParameters.From.HasValue)
            {
                query = query.Where(eventItem =>
                    eventItem.StartAt >= queryParameters.From.Value);
            }

            if (queryParameters.To.HasValue)
            {
                query = query.Where(eventItem =>
                    eventItem.EndAt <= queryParameters.To.Value);
            }

            query = query.OrderBy(eventItem => eventItem.StartAt);

            var totalCount = query.Count();

            var items = query
                .Skip(
                    (queryParameters.Page - 1) *
                    queryParameters.PageSize)
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
            return _eventStore.GetById(id)
                ?? throw new NotFoundException(
                    $"Event with id '{id}' was not found.");
        }

        public Event Create(Event newEvent)
        {
            ValidateEvent(newEvent);

            try
            {
                _eventStore.Add(newEvent);
            }
            catch (InvalidOperationException)
            {
                throw new ValidationException(
                    $"Event with id '{newEvent.Id}' already exists.");
            }

            return newEvent;
        }

        public void Update(Guid id, Event updatedEvent)
        {
            var existingEvent = GetById(id);

            ValidateEvent(updatedEvent);

            var reservedSeats =
                existingEvent.TotalSeats -
                existingEvent.AvailableSeats;

            if (updatedEvent.TotalSeats < reservedSeats)
            {
                throw new ValidationException(
                    "TotalSeats cannot be less than reserved seats.");
            }

            updatedEvent.Id = id;
            updatedEvent.AvailableSeats =
                updatedEvent.TotalSeats - reservedSeats;

            _eventStore.Update(updatedEvent);
        }

        public void Delete(Guid id)
        {
            if (!_eventStore.Delete(id))
            {
                throw new NotFoundException(
                    $"Event with id '{id}' was not found.");
            }
        }

        private static void ValidateEvent(Event eventItem)
        {
            if (string.IsNullOrWhiteSpace(eventItem.Title))
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
