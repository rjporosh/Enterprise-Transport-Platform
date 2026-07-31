namespace BookingService.Application.Common.Models;

/// <summary>
/// Lightweight result wrapper so handlers can return an expected business
/// failure (e.g. "seat unavailable") without throwing for control flow,
/// while still throwing for truly exceptional cases (see DomainException).
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
