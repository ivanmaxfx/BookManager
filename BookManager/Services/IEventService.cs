using MyWebApiProject.Dtos;
using MyWebApiProject.Models;

namespace MyWebApiProject.Services
{
    /// <summary>
    /// Сервис для работы с мероприятиями.
    /// </summary>
    public interface IEventService
    {
        PaginatedResult<Event> GetAll(EventQueryParameters queryParameters);

        Event GetById(Guid id);

        Event Create(Event newEvent);

        void Update(Guid id, Event updatedEvent);

        void Delete(Guid id);
    }
}