namespace AuthService.Application.Common.Models;

public sealed class Result
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public IReadOnlyList<ErrorDetail> Errors { get; }
    public string? TraceId { get; }

    private Result(bool isSuccess, string message, IReadOnlyList<ErrorDetail> errors, string? traceId)
    {
        IsSuccess = isSuccess;
        Message = message;
        Errors = errors;
        TraceId = traceId;
    }

    public static Result Success() => new(true, string.Empty, Array.Empty<ErrorDetail>(), null);
    public static Result Failure(string message, params ErrorDetail[] errors) => new(false, message, errors, null);
    public static Result Failure(string message, IEnumerable<ErrorDetail> errors, string? traceId = null) => new(false, message, errors.ToList(), traceId);
}

public sealed record ErrorDetail(string Code, string Field, string Message);
