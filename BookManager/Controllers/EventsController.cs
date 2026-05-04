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

        /// <summary>
        /// Инициализирует новый экземпляр контроллера мероприятий.
        /// </summary>
        /// <param name="eventService">Сервис для работы с мероприятиями.</param>
        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        /// <summary>
        /// Получить список мероприятий с фильтрацией и пагинацией.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<Event>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public ActionResult<PaginatedResult<Event>> GetAll([FromQuery] EventQueryParameters queryParameters)
        {
            var result = _eventService.GetAll(queryParameters);
            return Ok(result);
        }

        /// <summary>
        /// Получить мероприятие по идентификатору.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public ActionResult<Event> GetById(Guid id)
        {
            var eventItem = _eventService.GetById(id);
            return Ok(eventItem);
        }

        /// <summary>
        /// Создать новое мероприятие.
        /// </summary>
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

            var createdEvent = _eventService.Create(newEvent);

            return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
        }

        /// <summary>
        /// Обновить мероприятие по идентификатору.
        /// </summary>
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

            _eventService.Update(id, updatedEvent);

            return NoContent();
        }

        /// <summary>
        /// Удалить мероприятие по идентификатору.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public IActionResult Delete(Guid id)
        {
            _eventService.Delete(id);
            return NoContent();
        }
    }
}