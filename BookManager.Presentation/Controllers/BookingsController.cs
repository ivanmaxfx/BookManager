using System.Security.Claims;
using BookManager.Application.Dtos;
using BookManager.Application.Services;
using BookManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManager.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("bookings")]
    [Produces("application/json")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(
            IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(
            typeof(BookingInfo),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingInfo>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            return Ok(
                await _bookingService.GetBookingByIdAsync(
                    id,
                    cancellationToken));
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
            StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userIdValue =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(
                    userIdValue,
                    out var userId))
            {
                return Unauthorized();
            }

            var roleValue =
                User.FindFirst(
                    ClaimTypes.Role)?.Value;

            if (!Enum.TryParse<UserRole>(
                    roleValue,
                    true,
                    out var role))
            {
                return Forbid();
            }

            await _bookingService.CancelBookingAsync(
                id,
                userId,
                role,
                cancellationToken);

            return NoContent();
        }
    }
}
