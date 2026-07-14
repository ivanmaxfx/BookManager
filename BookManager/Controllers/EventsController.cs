using Microsoft.AspNetCore.Mvc;
using MyWebApiProject.Dtos;
using MyWebApiProject.Models;
using MyWebApiProject.Services;

namespace MyWebApiProject.Controllers
{
    [ApiController]
    [Route("events")]
    [Produces("application/json")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(
            IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        [ProducesResponseType(
            typeof(PaginatedResult<EventInfo>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status400BadRequest)]
        public async Task<
            ActionResult<PaginatedResult<EventInfo>>> GetAll(
            [FromQuery] EventQueryParameters queryParameters,
            CancellationToken cancellationToken)
        {
            var result = await _eventService.GetAllAsync(
                queryParameters,
                cancellationToken);

            var response =
                new PaginatedResult<EventInfo>
                {
                    TotalCount = result.TotalCount,
                    Page = result.Page,
                    PageSize = result.PageSize,
                    Items = result.Items
                        .Select(EventInfo.FromEvent)
                        .ToList()
                };

            return Ok(response);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(
            typeof(EventInfo),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventInfo>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var eventItem =
                await _eventService.GetByIdAsync(
                    id,
                    cancellationToken);

            return Ok(
                EventInfo.FromEvent(eventItem));
        }

        [HttpPost]
        [ProducesResponseType(
            typeof(EventInfo),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            typeof(ValidationProblemDetails),
            StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EventInfo>> Create(
            [FromBody] EventRequest request,
            CancellationToken cancellationToken)
        {
            var newEvent = Event.Create(
                request.Title,
                request.Description,
                request.StartAt!.Value,
                request.EndAt!.Value,
                request.TotalSeats!.Value);

            var createdEvent =
                await _eventService.CreateAsync(
                    newEvent,
                    cancellationToken);

            var response =
                EventInfo.FromEvent(createdEvent);

            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id },
                response);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            typeof(ValidationProblemDetails),
            StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] EventRequest request,
            CancellationToken cancellationToken)
        {
            var updatedEvent = Event.Create(
                request.Title,
                request.Description,
                request.StartAt!.Value,
                request.EndAt!.Value,
                request.TotalSeats!.Value);

            await _eventService.UpdateAsync(
                id,
                updatedEvent,
                cancellationToken);

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            await _eventService.DeleteAsync(
                id,
                cancellationToken);

            return NoContent();
        }
    }
}
