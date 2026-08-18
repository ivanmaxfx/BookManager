using System.Security.Claims;
using Bookings.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Presentation;

[Authorize]
[ApiController]
public sealed class BookingsController :
    ControllerBase
{
    private readonly BookingService _bookings;

    public BookingsController(
        BookingService bookings)
    {
        _bookings = bookings;
    }

    [HttpPost("events/{eventId:guid}/book")]
    public async Task<ActionResult<BookingDto>>
        Create(
            Guid eventId,
            CancellationToken cancellationToken)
    {
        var id =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(id, out var userId))
        {
            return Unauthorized();
        }

        var booking =
            await _bookings.CreateAsync(
                eventId,
                userId,
                cancellationToken);

        return Accepted(booking);
    }

    [HttpGet("bookings/{id:guid}")]
    public async Task<ActionResult<BookingDto>>
        GetById(
            Guid id,
            CancellationToken cancellationToken)
    {
        return Ok(
            await _bookings.GetAsync(
                id,
                cancellationToken));
    }

    [HttpDelete("bookings/{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                value,
                out var userId))
        {
            return Unauthorized();
        }

        var isAdmin =
            User.IsInRole("Admin");

        await _bookings.CancelAsync(
            id,
            userId,
            isAdmin,
            cancellationToken);

        return NoContent();
    }
}
