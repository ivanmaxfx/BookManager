using Microsoft.AspNetCore.Mvc;
using MyWebApiProject.Dtos;
using MyWebApiProject.Models;
using MyWebApiProject.Services;

namespace MyWebApiProject.Controllers
{
    /// <summary>
    /// API для управления мероприятиями.
    /// </summary>
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

        /// <summary>
        /// Получить список всех мероприятий.
        /// </summary>
        /// <returns>Список мероприятий.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<Event>), StatusCodes.Status200OK)]
        public ActionResult<IReadOnlyCollection<Event>> GetAll()
        {
            var events = _eventService.GetAll();
            return Ok(events);
        }

        /// <summary>
        /// Получить мероприятие по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор мероприятия.</param>
        /// <returns>Найденное мероприятие.</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public ActionResult<Event> GetById(Guid id)
        {
            var eventItem = _eventService.GetById(id);

            if (eventItem is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Event not found",
                    Detail = $"Event with id '{id}' was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(eventItem);
        }

        /// <summary>
        /// Создать новое мероприятие.
        /// </summary>
        /// <param name="request">Данные для создания мероприятия.</param>
        /// <returns>Созданное мероприятие.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public ActionResult<Event> Create([FromBody] EventRequest request)
        {
            var newEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                Description = request.Description,
                StartAt = request.StartAt!.Value,
                EndAt = request.EndAt!.Value
            };

            _eventService.Create(newEvent);

            return CreatedAtAction(
                nameof(GetById),
                new { id = newEvent.Id },
                newEvent);
        }

        /// <summary>
        /// Полностью обновить мероприятие по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор мероприятия.</param>
        /// <param name="request">Новые данные мероприятия.</param>
        /// <returns>Пустой ответ при успешном обновлении.</returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public IActionResult Update(Guid id, [FromBody] EventRequest request)
        {
            var updatedEvent = new Event
            {
                Id = id,
                Title = request.Title.Trim(),
                Description = request.Description,
                StartAt = request.StartAt!.Value,
                EndAt = request.EndAt!.Value
            };

            var updated = _eventService.Update(id, updatedEvent);

            if (!updated)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Event not found",
                    Detail = $"Event with id '{id}' was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return NoContent();
        }

        /// <summary>
        /// Удалить мероприятие по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор мероприятия.</param>
        /// <returns>Пустой ответ при успешном удалении.</returns>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public IActionResult Delete(Guid id)
        {
            var deleted = _eventService.Delete(id);

            if (!deleted)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Event not found",
                    Detail = $"Event with id '{id}' was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return NoContent();
        }
    }
}