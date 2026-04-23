using System.Collections.Concurrent;
using MyWebApiProject.Models;

namespace MyWebApiProject.Services
{
    public class EventService : IEventService
    {
        private readonly ConcurrentDictionary<Guid, Event> _events = new();

        public IReadOnlyCollection<Event> GetAll()
        {
            return _events.Values
                .OrderBy(e => e.StartAt)
                .ToList();
        }

        public Event? GetById(Guid id)
        {
            return _events.TryGetValue(id, out var eventItem)
                ? eventItem
                : null;
        }

        public Event Create(Event newEvent)
        {
            _events.TryAdd(newEvent.Id, newEvent);
            return newEvent;
        }

        public bool Update(Guid id, Event updatedEvent)
        {
            while (true)
            {
                if (!_events.TryGetValue(id, out var existingEvent))
                {
                    return false;
                }

                if (_events.TryUpdate(id, updatedEvent, existingEvent))
                {
                    return true;
                }
            }
        }

        public bool Delete(Guid id)
        {
            return _events.TryRemove(id, out _);
        }
    }
}