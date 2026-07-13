using System.Collections.Concurrent;
using MyWebApiProject.Models;

namespace MyWebApiProject.DataAccess
{
    public class InMemoryEventStore : IEventStore
    {
        private readonly ConcurrentDictionary<Guid, Event> _events = new();

        public IReadOnlyCollection<Event> GetAll()
        {
            return _events.Values.ToList();
        }

        public Event? GetById(Guid id)
        {
            return _events.TryGetValue(id, out var eventItem)
                ? eventItem
                : null;
        }

        public void Add(Event eventItem)
        {
            ArgumentNullException.ThrowIfNull(eventItem);

            if (!_events.TryAdd(eventItem.Id, eventItem))
            {
                throw new InvalidOperationException(
                    $"Event with id '{eventItem.Id}' already exists.");
            }
        }

        public void Update(Event eventItem)
        {
            ArgumentNullException.ThrowIfNull(eventItem);

            while (true)
            {
                if (!_events.TryGetValue(eventItem.Id, out var currentEvent))
                {
                    throw new InvalidOperationException(
                        $"Event with id '{eventItem.Id}' does not exist.");
                }

                if (_events.TryUpdate(
                    eventItem.Id,
                    eventItem,
                    currentEvent))
                {
                    return;
                }
            }
        }

        public bool Delete(Guid id)
        {
            return _events.TryRemove(id, out _);
        }
    }
}
