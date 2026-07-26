using Microsoft.AspNetCore.Mvc;
using MyWebApiProject.Dtos;
using MyWebApiProject.Services;

namespace MyWebApiProject.Controllers
{
    /// <summary>
    /// API для получения информации о бронированиях.
    /// </summary>
    [ApiController]
    [Route("bookings")]
    [Produces("application/json")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>
        /// Получить текущее состояние бронирования.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(
            typeof(BookingInfo),
            StatusCodes.Status200OK)]
        [ProducesResponseType(
            typeof(ProblemDetails),
            StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingInfo>> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var booking =
                await _bookingService.GetBookingByIdAsync(
                    id,
                    cancellationToken);

            return Ok(booking);
        }
    }
}
