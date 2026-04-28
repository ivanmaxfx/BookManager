using MyWebApiProject.Models;

namespace MyWebApiProject.Services
{
    public interface IEventService
    {
        IReadOnlyCollection<Event> GetAll();

        Event? GetById(Guid id);

        Event Create(Event newEvent);

        bool Update(Guid id, Event updatedEvent);

        bool Delete(Guid id);
    }
}