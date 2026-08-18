using Events.Domain;

namespace Events.Presentation;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ApiExceptionMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainValidationException exception)
        {
            context.Response.StatusCode = 400;

            await context.Response.WriteAsJsonAsync(
                new
                {
                    status = 400,
                    error = exception.Message
                });
        }
        catch (NotFoundException exception)
        {
            context.Response.StatusCode = 404;

            await context.Response.WriteAsJsonAsync(
                new
                {
                    status = 404,
                    error = exception.Message
                });
        }
    }
}
