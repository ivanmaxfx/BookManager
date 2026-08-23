using System.Text.Json;
using BookManager.Contracts;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Events.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Events.Infrastructure;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } =
        string.Empty;

    public string ConsumerGroup { get; set; } =
        "events-service";
}

public sealed class KafkaTopicInitializer :
    IHostedService
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaTopicInitializer>
        _logger;

    public KafkaTopicInitializer(
        KafkaOptions options,
        ILogger<KafkaTopicInitializer> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var admin =
                new AdminClientBuilder(
                    new AdminClientConfig
                    {
                        BootstrapServers =
                            _options.BootstrapServers
                    })
                    .Build();

            await admin.CreateTopicsAsync(
                new[]
                {
                    new TopicSpecification
                    {
                        Name =
                            KafkaTopics
                                .BookingConfirmed,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    }
                });

            _logger.LogInformation(
                "Kafka topic {Topic} created",
                KafkaTopics.BookingConfirmed);
        }
        catch (CreateTopicsException exception)
            when (exception.Results.Any(
                x =>
                    x.Error.Code ==
                    ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation(
                "Kafka topic {Topic} already exists",
                KafkaTopics.BookingConfirmed);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Kafka topic initialization failed; service will continue");
        }
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class BookingConfirmedConsumer :
    BackgroundService
{
    private readonly IServiceScopeFactory
        _scopeFactory;

    private readonly KafkaOptions _options;

    private readonly ILogger<
        BookingConfirmedConsumer> _logger;

    public BookingConfirmedConsumer(
        IServiceScopeFactory scopeFactory,
        KafkaOptions options,
        ILogger<BookingConfirmedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Consume() is blocking. Yield first so host startup
        // is not blocked by the Kafka consumer loop.
        await Task.Yield();

        var config =
            new ConsumerConfig
            {
                BootstrapServers =
                    _options.BootstrapServers,

                GroupId =
                    _options.ConsumerGroup,

                AutoOffsetReset =
                    AutoOffsetReset.Earliest,

                EnableAutoCommit = false
            };

        using var consumer =
            new ConsumerBuilder<string, string>(
                config)
                .Build();

        consumer.Subscribe(
            KafkaTopics.BookingConfirmed);

        _logger.LogInformation(
            "Subscribed to {Topic}",
            KafkaTopics.BookingConfirmed);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result =
                null;

            try
            {
                result =
                    consumer.Consume(stoppingToken);

                var message =
                    JsonSerializer.Deserialize<
                        BookingConfirmed>(
                        result.Message.Value);

                if (message is null)
                {
                    _logger.LogWarning(
                        "Empty BookingConfirmed message");

                    continue;
                }

                await HandleAsync(
                    message,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken
                    .IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to process Kafka message");
            }
            finally
            {
                if (result is not null)
                {
                    try
                    {
                        consumer.Commit(result);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(
                            exception,
                            "Kafka offset commit failed");
                    }
                }
            }
        }

        consumer.Close();
    }

    private async Task HandleAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    EventsDbContext>();

        var cache =
            scope.ServiceProvider
                .GetRequiredService<
                    ICacheService>();

        var alreadyProcessed =
            await db.ProcessedBookingEvents
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.BookingId ==
                        message.BookingId,
                    cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Booking {BookingId} already processed",
                message.BookingId);

            return;
        }

        var eventItem =
            await db.Events.FirstOrDefaultAsync(
                x =>
                    x.Id ==
                    message.EventId,
                cancellationToken);

        if (eventItem is null)
        {
            _logger.LogWarning(
                "Event {EventId} not found for booking {BookingId}",
                message.EventId,
                message.BookingId);

            return;
        }

        if (!eventItem.TryReserveSeats(
                message.SeatCount))
        {
            _logger.LogWarning(
                "Event {EventId} has no free seats for booking {BookingId}",
                message.EventId,
                message.BookingId);

            return;
        }

        db.ProcessedBookingEvents.Add(
            new ProcessedBookingEvent
            {
                BookingId =
                    message.BookingId,

                ProcessedAt =
                    DateTime.UtcNow
            });

        await db.SaveChangesAsync(
            cancellationToken);

        // The database is authoritative:
        // invalidate cache only after commit.
        await cache.RemoveAsync(
            EventCacheKeys.ById(
                message.EventId),
            cancellationToken);

        _logger.LogInformation(
            "Booking {BookingId} processed; event {EventId} seats decreased by {SeatCount}",
            message.BookingId,
            message.EventId,
            message.SeatCount);
    }
}
