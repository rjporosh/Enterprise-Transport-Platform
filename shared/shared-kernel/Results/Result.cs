using System.Collections.ObjectModel;

namespace Platform.SharedKernel.Results;

/// <summary>
/// Outcome of an operation: success, or failure carrying ONE OR MORE
/// <see cref="Error"/>s. Never stops at the first error — validators aggregate.
/// </summary>
public class Result
{
    private static readonly IReadOnlyList<Error> NoErrors = new ReadOnlyCollection<Error>([]);

    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        if (isSuccess && errors.Count > 0)
            throw new InvalidOperationException("A successful result cannot carry errors.");
        if (!isSuccess && errors.Count == 0)
            throw new InvalidOperationException("A failed result must carry at least one error.");

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>The first error, for callers that only need one (e.g. status-code mapping).</summary>
    public Error? PrimaryError => Errors.Count > 0 ? Errors[0] : null;

    public static Result Success() => new(true, NoErrors);
    public static Result Failure(params Error[] errors) => new(false, errors);
    public static Result Failure(IEnumerable<Error> errors) => new(false, errors.ToArray());

    public static Result<T> Success<T>(T value) => Result<T>.FromValue(value);
    public static Result<T> Failure<T>(params Error[] errors) => Result<T>.FromErrors(errors);
    public static Result<T> Failure<T>(IEnumerable<Error> errors) => Result<T>.FromErrors(errors.ToArray());
}

/// <summary>A <see cref="Result"/> that carries a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(true, [])
    {
        _value = value;
    }

    private Result(IReadOnlyList<Error> errors) : base(false, errors) { }

    /// <summary>The value. Throws if the result is a failure — check <see cref="Result.IsSuccess"/> first.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    internal static Result<T> FromValue(T value) => new(value);
    internal static Result<T> FromErrors(IReadOnlyList<Error> errors) => new(errors);

    public static implicit operator Result<T>(T value) => FromValue(value);
    public static implicit operator Result<T>(Error error) => FromErrors([error]);
}
