using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyWebApiProject.Exceptions;

namespace MyWebApiProject.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
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
            var (statusCode, title) = exception switch
            {
                ValidationException => (
                    StatusCodes.Status400BadRequest,
                    "Validation error"),

                NotFoundException => (
                    StatusCodes.Status404NotFound,
                    "Resource not found"),

                NoAvailableSeatsException => (
                    StatusCodes.Status409Conflict,
                    "No available seats"),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Internal server error")
            };

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            };

            context.Response.ContentType =
                "application/problem+json";

            context.Response.StatusCode = statusCode;

            var json = JsonSerializer.Serialize(
                problemDetails);

            await context.Response.WriteAsync(json);
        }
    }
}
