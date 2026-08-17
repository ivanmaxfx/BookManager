using Microsoft.Extensions.Options;
using BookManager.Application.Abstractions.Persistence;
using BookManager.Domain.Entities;
using BookManager.Domain.Enums;
using BookManager.Presentation.Options;

namespace BookManager.Presentation.BackgroundServices
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

                    await Task.WhenAll(processingTasks);
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

        private async Task<IReadOnlyCollection<Guid>>
            GetPendingBookingIdsAsync(
                CancellationToken cancellationToken)
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var repository = scope.ServiceProvider
                .GetRequiredService<IBookingRepository>();

            return await repository.GetPendingIdsAsync(
                cancellationToken);
        }

        private async Task ProcessBookingAsync(
            Guid bookingId,
            CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(
                    _processingDelay,
                    stoppingToken);

                await using var scope =
                    _scopeFactory.CreateAsyncScope();

                var bookingRepository =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IBookingRepository>();

                var eventRepository =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IEventRepository>();

                var unitOfWork =
                    scope.ServiceProvider
                        .GetRequiredService<IUnitOfWork>();

                var booking =
                    await bookingRepository.GetByIdAsync(
                        bookingId,
                        trackChanges: true,
                        stoppingToken);

                if (booking is null ||
                    booking.Status != BookingStatus.Pending)
                {
                    return;
                }

                var eventExists =
                    await eventRepository.ExistsAsync(
                        booking.EventId,
                        stoppingToken);

                if (!eventExists)
                {
                    booking.Reject();
                    bookingRepository.Update(booking);

                    await unitOfWork.SaveChangesAsync(
                        stoppingToken);

                    _logger.LogWarning(
                        "Booking {BookingId} rejected because event {EventId} was not found",
                        booking.Id,
                        booking.EventId);

                    return;
                }

                booking.Confirm();
                bookingRepository.Update(booking);

                await unitOfWork.SaveChangesAsync(
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

                var bookingRepository =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IBookingRepository>();

                var eventRepository =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IEventRepository>();

                var unitOfWork =
                    scope.ServiceProvider
                        .GetRequiredService<IUnitOfWork>();

                var booking =
                    await bookingRepository.GetByIdAsync(
                        bookingId,
                        trackChanges: true,
                        cancellationToken);

                if (booking is null ||
                    booking.Status != BookingStatus.Pending)
                {
                    return;
                }

                var eventItem =
                    await eventRepository.GetByIdAsync(
                        booking.EventId,
                        trackChanges: true,
                        cancellationToken);

                booking.Reject();
                bookingRepository.Update(booking);

                if (eventItem is not null)
                {
                    eventItem.ReleaseSeats();
                    eventRepository.Update(eventItem);
                }

                await unitOfWork.SaveChangesAsync(
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
