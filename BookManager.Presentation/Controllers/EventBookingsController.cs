using System.Security.Claims;
using BookManager.Application.Dtos;
using BookManager.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManager.Presentation.Controllers
{
    [ApiController]
    [Route("events")]
    [Produces("application/json")]
    public class EventBookingsController :
        ControllerBase
    {
        private readonly IBookingService _bookingService;

        public EventBookingsController(
            IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [Authorize]
        [HttpPost("{id:guid}/book")]
        [ProducesResponseType(
            typeof(BookingInfo),
            StatusCodes.Status202Accepted)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status409Conflict)]
        public async Task<ActionResult<BookingInfo>>
            CreateBooking(
                Guid id,
                CancellationToken cancellationToken)
        {
            var value =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(value, out var userId))
            {
                return Unauthorized();
            }

            var booking =
                await _bookingService.CreateBookingAsync(
                    id,
                    userId,
                    cancellationToken);

            return AcceptedAtAction(
                nameof(BookingsController.GetById),
                "Bookings",
                new { id = booking.Id },
                booking);
        }
    }
}
