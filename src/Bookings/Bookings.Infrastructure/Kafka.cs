using System.Text.Json;
using BookManager.Contracts;
using Bookings.Application;
using Bookings.Domain;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bookings.Infrastructure;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } =
        string.Empty;
}

public sealed class BookingProcessingOptions
{
    public int PollIntervalMilliseconds { get; set; } =
        1000;

    public int ProcessingDelayMilliseconds { get; set; } =
        2000;
}

public sealed class KafkaBookingEventPublisher :
    IBookingEventPublisher,
    IDisposable
{
    private readonly IProducer<string, string>
        _producer;

    public KafkaBookingEventPublisher(
        KafkaOptions options)
    {
        _producer =
            new ProducerBuilder<string, string>(
                new ProducerConfig
                {
                    BootstrapServers =
                        options.BootstrapServers,

                    Acks = Acks.All
                })
                .Build();
    }

    public async Task PublishConfirmedAsync(
        BookingConfirmed message,
        CancellationToken cancellationToken = default)
    {
        var value =
            JsonSerializer.Serialize(message);

        await _producer.ProduceAsync(
            KafkaTopics.BookingConfirmed,
            new Message<string, string>
            {
                // EventId keeps all bookings of one
                // event ordered in one partition.
                Key =
                    message.EventId.ToString(),

                Value = value
            },
            cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(
            TimeSpan.FromSeconds(5));

        _producer.Dispose();
    }
}

public sealed class BookingConfirmationWorker :
    BackgroundService
{
    private readonly IServiceScopeFactory
        _scopeFactory;

    private readonly IBookingEventPublisher
        _publisher;

    private readonly BookingProcessingOptions
        _options;

    private readonly ILogger<
        BookingConfirmationWorker> _logger;

    public BookingConfirmationWorker(
        IServiceScopeFactory scopeFactory,
        IBookingEventPublisher publisher,
        BookingProcessingOptions options,
        ILogger<BookingConfirmationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope =
                    _scopeFactory
                        .CreateAsyncScope();

                var repository =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IBookingRepository>();

                var pending =
                    await repository
                        .GetPendingIdsAsync(
                            stoppingToken);

                foreach (var id in pending)
                {
                    await ProcessAsync(
                        id,
                        stoppingToken);
                }
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
                    "Booking confirmation cycle failed");
            }

            await Task.Delay(
                _options
                    .PollIntervalMilliseconds,
                stoppingToken);
        }
    }

    private async Task ProcessAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        await Task.Delay(
            _options
                .ProcessingDelayMilliseconds,
            cancellationToken);

        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<
                    IBookingRepository>();

        var booking =
            await repository.GetByIdAsync(
                bookingId,
                true,
                cancellationToken);

        if (booking is null ||
            booking.Status !=
                BookingStatus.Pending)
        {
            return;
        }

        booking.Confirm();

        // Requirement: persist own state first.
        await repository.SaveChangesAsync(
            cancellationToken);

        var confirmed =
            new BookingConfirmed(
                booking.Id,
                booking.EventId,
                booking.UserId,
                booking.SeatCount,
                booking.ProcessedAt
                    ?? DateTime.UtcNow);

        // Only after the database commit do we
        // publish the integration event.
        await _publisher.PublishConfirmedAsync(
            confirmed,
            cancellationToken);

        _logger.LogInformation(
            "Booking {BookingId} confirmed and BookingConfirmed published",
            booking.Id);
    }
}
