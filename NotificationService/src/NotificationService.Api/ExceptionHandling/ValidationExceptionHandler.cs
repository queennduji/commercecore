using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace NotificationService.Api.ExceptionHandling;

public class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            errors = validationException.Errors.Select(e => e.ErrorMessage)
        }, cancellationToken);

        return true;
    }
}
