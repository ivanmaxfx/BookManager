using BookManager.Application.Abstractions.Persistence;
using BookManager.Application.Dtos;
using BookManager.Domain.Exceptions;
using BookManager.Domain.Entities;

namespace BookManager.Application.Services
{
    public sealed class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IUnitOfWork _unitOfWork;

        public EventService(
            IEventRepository eventRepository,
            IUnitOfWork unitOfWork)
        {
            _eventRepository = eventRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PaginatedResult<Event>> GetAllAsync(
            EventQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
        {
            ValidateQueryParameters(queryParameters);

            var title = queryParameters.Title?.Trim();

            var totalCount =
                await _eventRepository.CountAsync(
                    title,
                    queryParameters.From,
                    queryParameters.To,
                    cancellationToken);

            var skip =
                (queryParameters.Page - 1) *
                queryParameters.PageSize;

            var items =
                await _eventRepository.GetPageAsync(
                    title,
                    queryParameters.From,
                    queryParameters.To,
                    skip,
                    queryParameters.PageSize,
                    cancellationToken);

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
            var eventItem =
                await _eventRepository.GetByIdAsync(
                    id,
                    trackChanges: false,
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

            await _eventRepository.AddAsync(
                newEvent,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return newEvent;
        }

        public async Task UpdateAsync(
            Guid id,
            Event updatedEvent,
            CancellationToken cancellationToken = default)
        {
            ValidateEvent(updatedEvent);

            var existingEvent =
                await _eventRepository.GetByIdAsync(
                    id,
                    trackChanges: true,
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

            _eventRepository.Update(existingEvent);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var eventItem =
                await _eventRepository.GetByIdAsync(
                    id,
                    trackChanges: true,
                    cancellationToken);

            if (eventItem is null)
            {
                throw new NotFoundException(
                    $"Event with id '{id}' was not found.");
            }

            _eventRepository.Remove(eventItem);

            await _unitOfWork.SaveChangesAsync(
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
