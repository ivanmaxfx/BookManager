using MyWebApiProject.Models;

namespace MyWebApiProject.DataAccess
{
    public interface IEventStore
    {
        IReadOnlyCollection<Event> GetAll();

        Event? GetById(Guid id);

        void Add(Event eventItem);

        void Update(Event eventItem);

        bool Delete(Guid id);
    }
}
