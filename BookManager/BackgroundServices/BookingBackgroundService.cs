using Microsoft.Extensions.Options;
using MyWebApiProject.DataAccess;
using MyWebApiProject.Models;
using MyWebApiProject.Options;

namespace MyWebApiProject.BackgroundServices
{
    public sealed class BookingBackgroundService : BackgroundService
    {
        private readonly IBookingStore _bookingStore;
        private readonly IEventStore _eventStore;
        private readonly ILogger<BookingBackgroundService> _logger;
        private readonly TimeSpan _pollingInterval;
        private readonly TimeSpan _processingDelay;

        private readonly SemaphoreSlim _processingSemaphore =
            new(1, 1);

        public BookingBackgroundService(
            IBookingStore bookingStore,
            IEventStore eventStore,
            IOptions<BookingProcessingOptions> options,
            ILogger<BookingBackgroundService> logger)
        {
            _bookingStore = bookingStore;
            _eventStore = eventStore;
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
                    var pendingBookings = _bookingStore
                        .GetPending()
                        .ToList();

                    var tasks = pendingBookings.Select(
                        booking => ProcessBookingAsync(
                            booking,
                            stoppingToken));

                    await Task.WhenAll(tasks);
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

        private async Task ProcessBookingAsync(
            Booking booking,
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Started processing booking {BookingId}",
                booking.Id);

            await Task.Delay(
                _processingDelay,
                stoppingToken);

            await _processingSemaphore.WaitAsync(
                stoppingToken);

            Event? eventItem = null;

            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                eventItem = _eventStore.GetById(
                    booking.EventId);

                if (eventItem is null)
                {
                    booking.Reject();
                    _bookingStore.Update(booking);

                    _logger.LogWarning(
                        "Booking {BookingId} rejected because event {EventId} was not found",
                        booking.Id,
                        booking.EventId);

                    return;
                }

                booking.Confirm();
                _bookingStore.Update(booking);

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
                    booking.Id);

                RejectBookingAndReleaseSeat(
                    booking,
                    eventItem);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }

        private void RejectBookingAndReleaseSeat(
            Booking booking,
            Event? eventItem)
        {
            if (booking.Status != BookingStatus.Pending)
            {
                return;
            }

            try
            {
                booking.Reject();

                if (eventItem is not null)
                {
                    eventItem.ReleaseSeats();
                    _eventStore.Update(eventItem);
                }

                _bookingStore.Update(booking);

                _logger.LogWarning(
                    "Booking {BookingId} rejected after processing error",
                    booking.Id);
            }
            catch (Exception recoveryException)
            {
                _logger.LogError(
                    recoveryException,
                    "Failed to reject booking {BookingId} after processing error",
                    booking.Id);
            }
        }

        public override void Dispose()
        {
            _processingSemaphore.Dispose();
            base.Dispose();
        }
    }
}
