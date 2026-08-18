using Bookings.Domain;

namespace Bookings.Presentation;

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
            await Write(
                context,
                400,
                exception.Message);
        }
        catch (NotFoundException exception)
        {
            await Write(
                context,
                404,
                exception.Message);
        }
        catch (ForbiddenException exception)
        {
            await Write(
                context,
                403,
                exception.Message);
        }
        catch (BookingLimitException exception)
        {
            await Write(
                context,
                409,
                exception.Message);
        }
    }

    private static async Task Write(
        HttpContext context,
        int status,
        string message)
    {
        context.Response.StatusCode = status;

        await context.Response.WriteAsJsonAsync(
            new
            {
                status,
                error = message
            });
    }
}
