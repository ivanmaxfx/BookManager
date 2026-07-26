using MyWebApiProject.Dtos;
using BookManager.Domain.Entities;
using BookManager.Domain.Enums;

namespace MyWebApiProject.Services
{
    public interface IEventService
    {
        Task<PaginatedResult<Event>> GetAllAsync(
            EventQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        Task<Event> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<Event> CreateAsync(
            Event newEvent,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Guid id,
            Event updatedEvent,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
