using System.Net;
using BookingService.Api.Diagnostics;
using BookingService.Domain.Exceptions;
using FluentValidation;

namespace BookingService.Api.Middleware;

/// <summary>
/// Central exception handler. Emits the platform's unified failure envelope
/// (<c>success:false</c>, <c>message</c>, <c>errors:[{code,field,message}]</c>,
/// <c>traceId</c>, <c>timestamp</c>) — every validation failure is returned,
/// never just the first. 5xx responses are also written, with a diagnosed
/// root cause + suggested fix, to
/// <c>logs/runtime-errors/runtime-error-dd-MM-yyyy.txt</c>. Never leaks stack
/// traces, SQL, or connection strings to the client.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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
        var correlationId = context.Items.TryGetValue("X-Correlation-Id", out var cid) ? cid?.ToString() : context.TraceIdentifier;

        var (statusCode, message, errors) = Map(exception);

        if ((int)statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path} (correlation {CorrelationId})",
                context.Request.Method, context.Request.Path, correlationId);
            RuntimeErrorLogWriter.Write(exception, _environment.ContentRootPath, _environment.EnvironmentName);
        }
        else
        {
            _logger.LogInformation("Handled {ExceptionType} as {StatusCode} on {Path}: {Message}",
                exception.GetType().Name, (int)statusCode, context.Request.Path, exception.Message);
        }

        var payload = new
        {
            success = false,
            message,
            errors,
            traceId = correlationId,
            timestamp = DateTimeOffset.UtcNow
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(payload);
    }

    private static (HttpStatusCode Status, string Message, IReadOnlyCollection<ApiError> Errors) Map(Exception exception) => exception switch
    {
        ValidationException v => (
            HttpStatusCode.BadRequest,
            "One or more validation errors occurred.",
            v.Errors.Select(e => new ApiError(e.ErrorCode ?? "validation.error", e.PropertyName, e.ErrorMessage)).ToList()),

        BookingNotFoundException => (HttpStatusCode.NotFound, exception.Message, One("booking.not_found", exception.Message)),
        TripNotFoundException => (HttpStatusCode.NotFound, exception.Message, One("trip.not_found", exception.Message)),
        SeatUnavailableException => (HttpStatusCode.Conflict, exception.Message, One("seat.unavailable", exception.Message)),
        InvalidBookingStateException => (HttpStatusCode.Conflict, exception.Message, One("booking.invalid_state", exception.Message)),
        DomainException => (HttpStatusCode.BadRequest, exception.Message, One("domain.rule_violated", exception.Message)),

        _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.", One("internal.error", "An unexpected error occurred."))
    };

    private static IReadOnlyCollection<ApiError> One(string code, string message) => [new ApiError(code, null, message)];

    private sealed record ApiError(string Code, string? Field, string Message);
}
