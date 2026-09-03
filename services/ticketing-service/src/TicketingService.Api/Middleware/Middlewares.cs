using System.Net;
using FluentValidation;
using Platform.SharedKernel.Correlation;

namespace TicketingService.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers.TryGetValue(Header, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString() : Guid.NewGuid().ToString();
        context.Response.Headers[Header] = id;
        context.Items[Header] = id;
        using (CorrelationContext.BeginScope(id))
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", id))
            await next(context);
    }
}

/// <summary>Unified failure envelope; every validation error returned; never leaks internals.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex) { await HandleAsync(context, ex); }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, message, errors) = ex switch
        {
            ValidationException v => (HttpStatusCode.BadRequest, "One or more validation errors occurred.",
                v.Errors.Select(e => new { code = e.ErrorCode ?? "validation.error", field = e.PropertyName, message = e.ErrorMessage }).Cast<object>().ToArray()),
            KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message, new object[] { new { code = "not_found", field = (string?)null, message = ex.Message } }),
            InvalidOperationException => (HttpStatusCode.Conflict, ex.Message, new object[] { new { code = "invalid_state", field = (string?)null, message = ex.Message } }),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", new object[] { new { code = "internal.error", field = (string?)null, message = "An unexpected error occurred." } })
        };

        if ((int)status >= 500)
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message,
            errors,
            traceId = context.Items["X-Correlation-Id"]?.ToString() ?? context.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
