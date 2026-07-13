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

        public EventsController(IEventService eventService)
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
        public ActionResult<PaginatedResult<EventInfo>> GetAll(
            [FromQuery] EventQueryParameters queryParameters)
        {
            var result = _eventService.GetAll(queryParameters);

            var response = new PaginatedResult<EventInfo>
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
        public ActionResult<EventInfo> GetById(Guid id)
        {
            var eventItem = _eventService.GetById(id);

            return Ok(EventInfo.FromEvent(eventItem));
        }

        [HttpPost]
        [ProducesResponseType(
            typeof(EventInfo),
            StatusCodes.Status201Created)]
        [ProducesResponseType(
            typeof(ValidationProblemDetails),
            StatusCodes.Status400BadRequest)]
        public ActionResult<EventInfo> Create(
            [FromBody] EventRequest request)
        {
            var newEvent = Event.Create(
                request.Title,
                request.Description,
                request.StartAt!.Value,
                request.EndAt!.Value,
                request.TotalSeats!.Value);

            var createdEvent = _eventService.Create(newEvent);
            var response = EventInfo.FromEvent(createdEvent);

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
        public IActionResult Update(
            Guid id,
            [FromBody] EventRequest request)
        {
            var updatedEvent = new Event
            {
                Id = id,
                Title = request.Title.Trim(),
                Description = request.Description,
                StartAt = request.StartAt!.Value,
                EndAt = request.EndAt!.Value,
                TotalSeats = request.TotalSeats!.Value
            };

            _eventService.Update(id, updatedEvent);

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(
            StatusCodes.Status204NoContent)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        public IActionResult Delete(Guid id)
        {
            _eventService.Delete(id);

            return NoContent();
        }
    }
}
