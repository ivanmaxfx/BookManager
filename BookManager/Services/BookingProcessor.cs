using Microsoft.Extensions.Options;
using MyWebApiProject.Options;

namespace MyWebApiProject.Services
{
    /// <summary>
    /// Выполняет отложенную обработку бронирований.
    /// </summary>
    public class BookingProcessor : IBookingProcessor
    {
        private readonly IBookingService _bookingService;
        private readonly BookingProcessingOptions _options;
        private readonly ILogger<BookingProcessor> _logger;

        public BookingProcessor(
            IBookingService bookingService,
            IOptions<BookingProcessingOptions> options,
            ILogger<BookingProcessor> logger)
        {
            _bookingService = bookingService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task ProcessPendingBookingsAsync(
            CancellationToken cancellationToken = default)
        {
            var pendingBookings =
                await _bookingService.GetPendingBookingsAsync(
                    cancellationToken);

            foreach (var booking in pendingBookings)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogInformation(
                        "Processing booking {BookingId}",
                        booking.Id);

                    await Task.Delay(
                        _options.ProcessingDelayMilliseconds,
                        cancellationToken);

                    await _bookingService.ConfirmBookingAsync(
                        booking.Id,
                        cancellationToken);

                    _logger.LogInformation(
                        "Booking {BookingId} confirmed",
                        booking.Id);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to process booking {BookingId}",
                        booking.Id);
                }
            }
        }
    }
}
