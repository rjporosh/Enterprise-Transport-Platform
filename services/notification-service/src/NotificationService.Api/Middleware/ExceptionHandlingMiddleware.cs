using System.Net;
using FluentValidation;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Api.Middleware;

/// <summary>
/// Centralized global exception handler (CLAUDE.md, "Centralized Exception
/// Handling"). Every unhandled exception is caught here, mapped to an RFC
/// 7807 ProblemDetails response (never exposing stack traces, connection
/// strings, or raw SQL to the client), and logged with full internal detail
/// -- which the Serilog file sink (see Program.cs) additionally persists to
/// logs/runtime-errors/runtime-error-yyyy-MM-dd.txt with timestamp,
/// endpoint, correlation id, exception type/message, and stack trace
/// (including file/line when portable PDBs are present, which they are by
/// default for a Debug or PublishReadyToRun=false Release build).
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
        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException => (
                HttpStatusCode.BadRequest,
                "One or more validation errors occurred.",
                (object?)validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            NotificationNotFoundException => (HttpStatusCode.NotFound, exception.Message, null),
            TemplateNotFoundException => (HttpStatusCode.NotFound, exception.Message, null),
            TemplateAlreadyExistsException => (HttpStatusCode.Conflict, exception.Message, null),
            InvalidNotificationStateException => (HttpStatusCode.Conflict, exception.Message, null),
            TemplateRenderException => (HttpStatusCode.UnprocessableEntity, exception.Message, null),
            DomainException => (HttpStatusCode.BadRequest, exception.Message, null),
            TimeoutException => (HttpStatusCode.GatewayTimeout, "A dependency did not respond in time.", null),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            // Structured, greppable fields -- Root Cause/Possible Solution are
            // deliberately generic here (an unhandled 500 by definition wasn't
            // anticipated); domain-specific dependency failures (SMTP, RabbitMQ,
            // DB, user-directory HTTP) log their own specific root
            // cause/solution at the point of failure (see e.g. SmtpEmailSender,
            // HttpUserDirectoryClient) before ever reaching this generic handler.
            _logger.LogError(exception,
                "Unhandled exception. Endpoint={Endpoint} Method={Method} Path={Path} CorrelationId={CorrelationId} " +
                "RootCause=Unclassified PossibleSolution=\"Inspect the stack trace below; if this recurs, add a " +
                "specific DomainException subtype or dependency-level catch so it is handled gracefully instead.\"",
                context.GetEndpoint()?.DisplayName, context.Request.Method, context.Request.Path, context.TraceIdentifier);
        }
        else
        {
            _logger.LogInformation("Handled {ExceptionType} as {StatusCode}: {Message}", exception.GetType().Name, (int)statusCode, exception.Message);
        }

        var problem = new
        {
            type = $"https://httpstatuses.io/{(int)statusCode}",
            title,
            status = (int)statusCode,
            traceId = context.TraceIdentifier,
            errors
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
