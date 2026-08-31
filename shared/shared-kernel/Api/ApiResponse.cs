namespace Platform.SharedKernel.Api;

using Platform.SharedKernel.Results;

/// <summary>
/// The single wire contract every platform API returns (.ai/MASTER-RULES.md §15,
/// "API Response Standard"). Services adopt this incrementally — it is provided
/// here so the gateway and future polyglot services share one shape.
/// </summary>
public sealed record ApiResponse<T>
{
    public required bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<ApiError> Errors { get; init; } = [];
    public string? TraceId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null, string? traceId = null) => new()
    {
        Success = true,
        Data = data,
        Message = message,
        TraceId = traceId
    };

    public static ApiResponse<T> Fail(IEnumerable<Error> errors, string? message = null, string? traceId = null) => new()
    {
        Success = false,
        Message = message ?? "The request could not be completed.",
        Errors = errors.Select(e => new ApiError(e.Code, e.Message, e.Field)).ToArray(),
        TraceId = traceId
    };
}

/// <summary>Non-generic body used for error-only responses (e.g. from the gateway).</summary>
public sealed record ApiResponse
{
    public required bool Success { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<ApiError> Errors { get; init; } = [];
    public string? TraceId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public static ApiResponse Fail(string code, string message, string? traceId = null) => new()
    {
        Success = false,
        Message = message,
        Errors = [new ApiError(code, message, null)],
        TraceId = traceId
    };
}

public sealed record ApiError(string Code, string Message, string? Field);
