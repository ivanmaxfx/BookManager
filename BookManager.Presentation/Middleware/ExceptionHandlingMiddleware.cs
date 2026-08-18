using System.Text.Json;
using BookManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BookManager.Presentation.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<
            ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception occurred.");

                await HandleExceptionAsync(
                    context,
                    exception);
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            var (status, title) =
                exception switch
                {
                    ValidationException => (
                        StatusCodes.Status400BadRequest,
                        "Validation error"),

                    EventAlreadyStartedException => (
                        StatusCodes.Status400BadRequest,
                        "Event already started"),

                    NotFoundException => (
                        StatusCodes.Status404NotFound,
                        "Resource not found"),

                    ForbiddenOperationException => (
                        StatusCodes.Status403Forbidden,
                        "Forbidden"),

                    NoAvailableSeatsException => (
                        StatusCodes.Status409Conflict,
                        "No available seats"),

                    BookingLimitExceededException => (
                        StatusCodes.Status409Conflict,
                        "Booking limit exceeded"),

                    _ => (
                        StatusCodes.Status500InternalServerError,
                        "Internal server error")
                };

            context.Response.StatusCode = status;
            context.Response.ContentType =
                "application/problem+json";

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(
                    new ProblemDetails
                    {
                        Status = status,
                        Title = title,
                        Detail = exception.Message
                    }));
        }
    }
}
