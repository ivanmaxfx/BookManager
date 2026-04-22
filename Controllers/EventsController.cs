using Microsoft.AspNetCore.Mvc;
using MyWebApiProject.Dtos;
using MyWebApiProject.Models;
using MyWebApiProject.Services;

namespace MyWebApiProject.Controllers
{
    [ApiController]
    [Route("events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        public ActionResult<List<Event>> GetAll()
        {
            var events = _eventService.GetAll();
            return Ok(events);
        }

        [HttpGet("{id:guid}")]
        public ActionResult<Event> GetById(Guid id)
        {
            var eventItem = _eventService.GetById(id);

            if (eventItem is null)
            {
                return NotFound(new { message = $"Event with id '{id}' not found." });
            }

            return Ok(eventItem);
        }

        [HttpPost]
        public ActionResult<Event> Create([FromBody] CreateEventRequest request)
        {
            if (request.EndAt <= request.StartAt)
            {
                return BadRequest(new { message = "EndAt must be later than StartAt." });
            }

            var newEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                StartAt = request.StartAt,
                EndAt = request.EndAt
            };

            _eventService.Create(newEvent);

            return CreatedAtAction(
                nameof(GetById),
                new { id = newEvent.Id },
                newEvent
            );
        }

        [HttpPut("{id:guid}")]
        public IActionResult Update(Guid id, [FromBody] UpdateEventRequest request)
        {
            if (request.EndAt <= request.StartAt)
            {
                return BadRequest(new { message = "EndAt must be later than StartAt." });
            }

            var updatedEvent = new Event
            {
                Id = id,
                Title = request.Title,
                Description = request.Description,
                StartAt = request.StartAt,
                EndAt = request.EndAt
            };

            var updated = _eventService.Update(id, updatedEvent);

            if (!updated)
            {
                return NotFound(new { message = $"Event with id '{id}' not found." });
            }

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public IActionResult Delete(Guid id)
        {
            var deleted = _eventService.Delete(id);

            if (!deleted)
            {
                return NotFound(new { message = $"Event with id '{id}' not found." });
            }

            return NoContent();
        }
    }
}