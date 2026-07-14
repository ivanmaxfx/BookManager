using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyWebApiProject.DataAccess;
using MyWebApiProject.Models;
using MyWebApiProject.Options;

namespace MyWebApiProject.BackgroundServices
{
    public sealed class BookingBackgroundService :
        BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingBackgroundService> _logger;
        private readonly TimeSpan _pollingInterval;
        private readonly TimeSpan _processingDelay;

        public BookingBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<BookingProcessingOptions> options,
            ILogger<BookingBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            _pollingInterval = TimeSpan.FromMilliseconds(
                options.Value.PollIntervalMilliseconds);

            _processingDelay = TimeSpan.FromMilliseconds(
                options.Value.ProcessingDelayMilliseconds);
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Booking background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pendingBookingIds =
                        await GetPendingBookingIdsAsync(
                            stoppingToken);

                    var processingTasks =
                        pendingBookingIds.Select(
                            bookingId =>
                                ProcessBookingAsync(
                                    bookingId,
                                    stoppingToken));

                    await Task.WhenAll(
                        processingTasks);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Booking processing cycle failed");
                }

                try
                {
                    await Task.Delay(
                        _pollingInterval,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Booking background service stopped");
        }

        private async Task<List<Guid>>
            GetPendingBookingIdsAsync(
                CancellationToken cancellationToken)
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var context = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            return await context.Bookings
                .AsNoTracking()
                .Where(booking =>
                    booking.Status ==
                    BookingStatus.Pending)
                .Select(booking => booking.Id)
                .ToListAsync(cancellationToken);
        }

        private async Task ProcessBookingAsync(
            Guid bookingId,
            CancellationToken stoppingToken)
        {
            try
            {
                // Задержки всех задач выполняются параллельно.
                await Task.Delay(
                    _processingDelay,
                    stoppingToken);

                await using var scope =
                    _scopeFactory.CreateAsyncScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var booking = await context.Bookings
                    .FirstOrDefaultAsync(
                        item => item.Id == bookingId,
                        stoppingToken);

                if (booking is null ||
                    booking.Status != BookingStatus.Pending)
                {
                    return;
                }

                var eventExists =
                    await context.Events.AnyAsync(
                        eventItem =>
                            eventItem.Id ==
                            booking.EventId,
                        stoppingToken);

                if (!eventExists)
                {
                    booking.Reject();

                    await context.SaveChangesAsync(
                        stoppingToken);

                    _logger.LogWarning(
                        "Booking {BookingId} rejected because event {EventId} was not found",
                        booking.Id,
                        booking.EventId);

                    return;
                }

                booking.Confirm();

                await context.SaveChangesAsync(
                    stoppingToken);

                _logger.LogInformation(
                    "Booking {BookingId} confirmed",
                    booking.Id);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to process booking {BookingId}",
                    bookingId);

                await RejectAndReleaseSeatAsync(
                    bookingId,
                    stoppingToken);
            }
        }

        private async Task RejectAndReleaseSeatAsync(
            Guid bookingId,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var scope =
                    _scopeFactory.CreateAsyncScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var booking = await context.Bookings
                    .FirstOrDefaultAsync(
                        item => item.Id == bookingId,
                        cancellationToken);

                if (booking is null ||
                    booking.Status != BookingStatus.Pending)
                {
                    return;
                }

                var eventItem = await context.Events
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id == booking.EventId,
                        cancellationToken);

                booking.Reject();

                eventItem?.ReleaseSeats();

                await context.SaveChangesAsync(
                    cancellationToken);

                _logger.LogWarning(
                    "Booking {BookingId} rejected after processing error",
                    bookingId);
            }
            catch (Exception recoveryException)
            {
                _logger.LogError(
                    recoveryException,
                    "Failed to recover booking {BookingId}",
                    bookingId);
            }
        }
    }
}
