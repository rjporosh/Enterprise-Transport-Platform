namespace NotificationService.Api.Common;

/// <summary>
/// The single response shape every endpoint in this service returns, success
/// or failure, per CLAUDE.md's "API Response Standard". ProblemDetails
/// (RFC 7807) is used ADDITIONALLY for truly unhandled exceptions (see
/// ExceptionHandlingMiddleware) — that combination (structured envelope for
/// expected outcomes, RFC 7807 for the unexpected) is deliberate: RFC 7807
/// is the .NET/HTTP ecosystem's standard shape for "something broke"
/// (Content-Type: application/problem+json, tooling recognizes it
/// out of the box), while the platform's own envelope is a friendlier,
/// frontend-designed contract for outcomes the API expected and validated.
/// </summary>
public sealed record ApiErrorItem(string Code, string Message, string? Field);

public sealed record ApiResponse<T>(bool Success, string Message, T? Data, IReadOnlyList<ApiErrorItem>? Errors, string TraceId, DateTimeOffset Timestamp)
{
    public static ApiResponse<T> Ok(T data, string traceId, string message = "Request completed successfully.") =>
        new(true, message, data, null, traceId, DateTimeOffset.UtcNow);

    public static ApiResponse<T> Fail(IReadOnlyList<ApiErrorItem> errors, string traceId, string message = "The request could not be completed.") =>
        new(false, message, default, errors, traceId, DateTimeOffset.UtcNow);
}
