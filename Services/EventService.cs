using MyWebApiProject.Models;

namespace MyWebApiProject.Services
{
    public class EventService : IEventService
    {
        private readonly List<Event> _events = new();

        public List<Event> GetAll()
        {
            return _events;
        }

        public Event? GetById(Guid id)
        {
            return _events.FirstOrDefault(e => e.Id == id);
        }

        public Event Create(Event newEvent)
        {
            _events.Add(newEvent);
            return newEvent;
        }

        public bool Update(Guid id, Event updatedEvent)
        {
            var existingEvent = GetById(id);

            if (existingEvent is null)
            {
                return false;
            }

            existingEvent.Title = updatedEvent.Title;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.StartAt = updatedEvent.StartAt;
            existingEvent.EndAt = updatedEvent.EndAt;

            return true;
        }

        public bool Delete(Guid id)
        {
            var existingEvent = GetById(id);

            if (existingEvent is null)
            {
                return false;
            }

            _events.Remove(existingEvent);
            return true;
        }
    }
}