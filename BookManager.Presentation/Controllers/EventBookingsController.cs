using Microsoft.AspNetCore.Mvc;
using BookManager.Application.Dtos;
using BookManager.Application.Services;

namespace BookManager.Presentation.Controllers
{
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

        [HttpPost("{id:guid}/book")]
        [ProducesResponseType(
            typeof(BookingInfo),
            StatusCodes.Status202Accepted)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status409Conflict)]
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
