using Events.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Events.Presentation;

[ApiController]
[Route("events")]
public sealed class EventsController :
    ControllerBase
{
    private readonly EventService _events;

    public EventsController(EventService events)
    {
        _events = events;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<
        IReadOnlyList<EventDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(
            await _events.GetAllAsync(
                cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EventDto>>
        GetById(
            Guid id,
            CancellationToken cancellationToken)
    {
        return Ok(
            await _events.GetByIdAsync(
                id,
                cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<EventDto>>
        Create(
            EventRequest request,
            CancellationToken cancellationToken)
    {
        var created =
            await _events.CreateAsync(
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        EventRequest request,
        CancellationToken cancellationToken)
    {
        await _events.UpdateAsync(
            id,
            request,
            cancellationToken);

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _events.DeleteAsync(
            id,
            cancellationToken);

        return NoContent();
    }
}
