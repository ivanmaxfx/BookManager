using Events.Application;
using Xunit;
using DomainEvent = Events.Domain.Event;

namespace Events.Application.Tests;

public sealed class EventServiceCacheTests
{
    [Fact]
    public async Task GetById_CacheHit_DoesNotCallRepository()
    {
        var item = CreateEvent();

        var repository =
            new FakeEventRepository(item);

        var cache =
            new FakeCache();

        var expected =
            EventDto.From(item);

        cache.Seed(
            EventCacheKeys.ById(item.Id),
            expected);

        var service =
            CreateService(repository, cache);

        var actual =
            await service.GetByIdAsync(item.Id);

        Assert.Equal(expected, actual);
        Assert.Equal(
            0,
            repository.GetByIdCalls);
    }

    [Fact]
    public async Task GetById_CacheMiss_LoadsDatabaseAndCaches()
    {
        var item = CreateEvent();

        var repository =
            new FakeEventRepository(item);

        var cache =
            new FakeCache();

        var service =
            CreateService(repository, cache);

        var actual =
            await service.GetByIdAsync(item.Id);

        Assert.Equal(item.Id, actual.Id);

        Assert.Equal(
            1,
            repository.GetByIdCalls);

        Assert.Equal(
            EventCacheKeys.ById(item.Id),
            cache.LastSetKey);

        Assert.Equal(
            TimeSpan.FromSeconds(300),
            cache.LastTtl);
    }

    [Fact]
    public async Task GetTop_CacheHit_DoesNotCallRepository()
    {
        var item = CreateEvent();

        var repository =
            new FakeEventRepository(item);

        var cache =
            new FakeCache();

        var expected =
            new List<EventDto>
            {
                EventDto.From(item)
            };

        cache.Seed(
            EventCacheKeys.Top10,
            expected);

        var service =
            CreateService(repository, cache);

        var actual =
            await service.GetTop10Async();

        Assert.Single(actual);

        Assert.Equal(
            0,
            repository.TopCalls);
    }

    [Fact]
    public async Task GetTop_CacheMiss_LoadsDatabaseAndCaches()
    {
        var item = CreateEvent();

        var repository =
            new FakeEventRepository(item);

        var cache =
            new FakeCache();

        var service =
            CreateService(repository, cache);

        var actual =
            await service.GetTop10Async();

        Assert.Single(actual);

        Assert.Equal(
            1,
            repository.TopCalls);

        Assert.Equal(
            EventCacheKeys.Top10,
            cache.LastSetKey);

        Assert.Equal(
            TimeSpan.FromSeconds(60),
            cache.LastTtl);
    }

    [Fact]
    public async Task Create_InvalidatesEventKey_AfterDatabaseSave()
    {
        var operations =
            new List<string>();

        var repository =
            new FakeEventRepository(
                null,
                operations);

        var cache =
            new FakeCache(operations);

        var service =
            CreateService(repository, cache);

        var created =
            await service.CreateAsync(
                Request("Created"));

        Assert.Equal(
            new[]
            {
                "save",
                $"remove:{EventCacheKeys.ById(created.Id)}"
            },
            operations);
    }

    [Fact]
    public async Task Update_InvalidatesEventKey_AfterDatabaseSave()
    {
        var item = CreateEvent();

        var operations =
            new List<string>();

        var repository =
            new FakeEventRepository(
                item,
                operations);

        var cache =
            new FakeCache(operations);

        var service =
            CreateService(repository, cache);

        await service.UpdateAsync(
            item.Id,
            Request("Updated"));

        Assert.Equal(
            new[]
            {
                "save",
                $"remove:{EventCacheKeys.ById(item.Id)}"
            },
            operations);
    }

    [Fact]
    public async Task Delete_InvalidatesEventKey_AfterDatabaseSave()
    {
        var item = CreateEvent();

        var operations =
            new List<string>();

        var repository =
            new FakeEventRepository(
                item,
                operations);

        var cache =
            new FakeCache(operations);

        var service =
            CreateService(repository, cache);

        await service.DeleteAsync(item.Id);

        Assert.Equal(
            new[]
            {
                "save",
                $"remove:{EventCacheKeys.ById(item.Id)}"
            },
            operations);
    }

    private static EventService CreateService(
        FakeEventRepository repository,
        FakeCache cache)
    {
        return new EventService(
            repository,
            cache,
            new EventCacheOptions
            {
                EventTtlSeconds = 300,
                Top10TtlSeconds = 60
            });
    }

    private static EventRequest Request(
        string title)
    {
        var start =
            DateTime.UtcNow.AddDays(2);

        return new EventRequest
        {
            Title = title,
            Description = "test",
            StartAt = start,
            EndAt = start.AddHours(2),
            TotalSeats = 10
        };
    }

    private static DomainEvent CreateEvent()
    {
        var request =
            Request("Event");

        return DomainEvent.Create(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats);
    }

    private sealed class FakeEventRepository :
        IEventRepository
    {
        private DomainEvent? _event;

        private readonly List<string>
            _operations;

        public FakeEventRepository(
            DomainEvent? item,
            List<string>? operations = null)
        {
            _event = item;

            _operations =
                operations ??
                new List<string>();
        }

        public int GetByIdCalls { get; private set; }

        public int TopCalls { get; private set; }

        public Task<IReadOnlyList<DomainEvent>>
            GetAllAsync(
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<DomainEvent> result =
                _event is null
                    ? Array.Empty<DomainEvent>()
                    : new[] { _event };

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<DomainEvent>>
            GetTop10PopularAsync(
                CancellationToken cancellationToken = default)
        {
            TopCalls++;

            IReadOnlyList<DomainEvent> result =
                _event is null
                    ? Array.Empty<DomainEvent>()
                    : new[] { _event };

            return Task.FromResult(result);
        }

        public Task<DomainEvent?> GetByIdAsync(
            Guid id,
            bool trackChanges,
            CancellationToken cancellationToken = default)
        {
            GetByIdCalls++;

            return Task.FromResult(
                _event?.Id == id
                    ? _event
                    : null);
        }

        public Task AddAsync(
            DomainEvent item,
            CancellationToken cancellationToken = default)
        {
            _event = item;

            return Task.CompletedTask;
        }

        public void Remove(DomainEvent item)
        {
            if (_event?.Id == item.Id)
            {
                _event = null;
            }
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            _operations.Add("save");

            return Task.CompletedTask;
        }
    }

    private sealed class FakeCache :
        ICacheService
    {
        private readonly Dictionary<
            string,
            object> _values =
                new();

        private readonly List<string>
            _operations;

        public FakeCache(
            List<string>? operations = null)
        {
            _operations =
                operations ??
                new List<string>();
        }

        public string? LastSetKey { get; private set; }

        public TimeSpan? LastTtl { get; private set; }

        public void Seed<T>(
            string key,
            T value)
        {
            _values[key] = value!;
        }

        public Task<T?> GetAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
        {
            if (_values.TryGetValue(
                    key,
                    out var value) &&
                value is T typed)
            {
                return Task.FromResult<T?>(
                    typed);
            }

            return Task.FromResult(
                default(T));
        }

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            _values[key] = value!;

            LastSetKey = key;
            LastTtl = ttl;

            return Task.CompletedTask;
        }

        public Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            _values.Remove(key);

            _operations.Add(
                $"remove:{key}");

            return Task.CompletedTask;
        }
    }
}
