using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using MyWebApiProject.Exceptions;
using MyWebApiProject.Middleware;

namespace EventService.Tests
{
    public class ExceptionHandlingMiddlewareTests
    {
        [Fact]
        public async Task NoAvailableSeatsException_Returns409Conflict()
        {
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new NoAvailableSeatsException(),
                NullLogger<ExceptionHandlingMiddleware>.Instance);

            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            context.Response.Body.Position = 0;

            using var document = await JsonDocument.ParseAsync(
                context.Response.Body);

            var root = document.RootElement;

            Assert.Equal(
                StatusCodes.Status409Conflict,
                context.Response.StatusCode);

            Assert.Equal(
                "application/problem+json",
                context.Response.ContentType);

            Assert.Equal(
                StatusCodes.Status409Conflict,
                root.GetProperty("status").GetInt32());

            Assert.Equal(
                "No available seats",
                root.GetProperty("title").GetString());

            Assert.Equal(
                "No available seats for this event",
                root.GetProperty("detail").GetString());
        }
    }
}
