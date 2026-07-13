using Microsoft.AspNetCore.Mvc;
using MyWebApiProject.Dtos;
using MyWebApiProject.Services;

namespace MyWebApiProject.Controllers
{
    /// <summary>
    /// API для создания бронирований мероприятий.
    /// </summary>
    [ApiController]
    [Route("events")]
    [Produces("application/json")]
    public class EventBookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public EventBookingsController(
            IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>
        /// Создать бронирование для мероприятия.
        /// </summary>
        [HttpPost("{id:guid}/book")]
        [ProducesResponseType(
            typeof(BookingInfo),
            StatusCodes.Status202Accepted)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingInfo>> CreateBooking(
            Guid id,
            CancellationToken cancellationToken)
        {
            var booking =
                await _bookingService.CreateBookingAsync(
                    id,
                    cancellationToken);

            return AcceptedAtAction(
                nameof(BookingsController.GetById),
                "Bookings",
                new { id = booking.Id },
                booking);
        }
    }
}
