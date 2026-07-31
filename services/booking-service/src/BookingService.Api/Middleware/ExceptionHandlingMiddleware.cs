using System.Net;
using BookingService.Domain.Exceptions;
using FluentValidation;

namespace BookingService.Api.Middleware;

/// <summary>
/// Translates exceptions into RFC 7807 ProblemDetails responses so every
/// error the API returns has a consistent, machine-parseable shape.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "One or more validation errors occurred."),
            BookingNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            TripNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            SeatUnavailableException => (HttpStatusCode.Conflict, exception.Message),
            InvalidBookingStateException => (HttpStatusCode.Conflict, exception.Message),
            DomainException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception processing {Path}", context.Request.Path);
        else
            _logger.LogInformation("Handled {ExceptionType} as {StatusCode}: {Message}", exception.GetType().Name, (int)statusCode, exception.Message);

        var problem = new
        {
            type = $"https://httpstatuses.io/{(int)statusCode}",
            title,
            status = (int)statusCode,
            traceId = context.TraceIdentifier,
            errors = exception is ValidationException validationException
                ? validationException.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                : null
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
