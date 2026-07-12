using Microsoft.Extensions.Options;
using MyWebApiProject.Options;
using MyWebApiProject.Services;

namespace MyWebApiProject.BackgroundServices
{
    /// <summary>
    /// Периодически запускает обработку ожидающих бронирований.
    /// </summary>
    public class BookingBackgroundService : BackgroundService
    {
        private readonly IBookingProcessor _bookingProcessor;
        private readonly BookingProcessingOptions _options;
        private readonly ILogger<BookingBackgroundService> _logger;

        public BookingBackgroundService(
            IBookingProcessor bookingProcessor,
            IOptions<BookingProcessingOptions> options,
            ILogger<BookingBackgroundService> logger)
        {
            _bookingProcessor = bookingProcessor;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Booking background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _bookingProcessor.ProcessPendingBookingsAsync(
                        stoppingToken);

                    await Task.Delay(
                        _options.PollIntervalMilliseconds,
                        stoppingToken);
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
                        "Unexpected booking background service error.");

                    await Task.Delay(
                        _options.PollIntervalMilliseconds,
                        stoppingToken);
                }
            }

            _logger.LogInformation(
                "Booking background service stopped.");
        }
    }
}
