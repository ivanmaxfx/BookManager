using System.Text.Json;
using Events.Application;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Events.Infrastructure;

public sealed class RedisCacheService :
    ICacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService>
        _logger;

    public RedisCacheService(
        IConnectionMultiplexer connection,
        ILogger<RedisCacheService> logger)
    {
        _database = connection.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        try
        {
            var value =
                await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(
                value.ToString());
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Redis GET failed for key {CacheKey}; falling back to database",
                key);

            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        try
        {
            var json =
                JsonSerializer.Serialize(value);

            await _database.StringSetAsync(
                key,
                json,
                ttl);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Redis SET failed for key {CacheKey}; request will continue without cache",
                key);
        }
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Redis DELETE failed for key {CacheKey}; request will continue",
                key);
        }
    }
}
