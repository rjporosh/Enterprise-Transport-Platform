namespace BusService.Application.Common.Models;

public class Result
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ResultError> Errors { get; set; } = new();
    public string? TraceId { get; set; }

    public static Result SuccessResult(string? message = null) => new()
    {
        Success = true,
        Message = message ?? "Operation completed successfully."
    };

    public static Result FailureResult(string message, IEnumerable<ResultError>? errors = null)
    {
        var result = new Result
        {
            Success = false,
            Message = message
        };
        if (errors is not null)
            result.Errors.AddRange(errors);
        return result;
    }
}

public class Result<T> : Result
{
    public T? Value { get; set; }

    public static Result<T> SuccessResult(T value, string? message = null) => new()
    {
        Success = true,
        Message = message ?? "Operation completed successfully.",
        Value = value
    };

    public new static Result<T> FailureResult(string message, IEnumerable<ResultError>? errors = null)
    {
        var result = new Result<T>
        {
            Success = false,
            Message = message
        };
        if (errors is not null)
            result.Errors.AddRange(errors);
        return result;
    }
}

public sealed record ResultError(string Code, string Field, string Message);
