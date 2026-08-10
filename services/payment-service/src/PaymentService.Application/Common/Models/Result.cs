namespace PaymentService.Application.Common.Models;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public IReadOnlyList<ResultError>? Errors { get; }

    private Result(bool isSuccess, T? value, string? error, IReadOnlyList<ResultError>? errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);

    public static Result<T> Failure(string error) => new(false, default, error, null);

    public static Result<T> Failure(IEnumerable<ResultError> errors) => new(false, default, null, errors.ToList());

    public static Result<T> Failure(string error, IEnumerable<ResultError> errors) => new(false, default, error, errors.ToList());
}
