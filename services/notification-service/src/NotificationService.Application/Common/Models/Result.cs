namespace NotificationService.Application.Common.Models;

/// <summary>
/// Result wrapper so handlers can return one or more *expected* business
/// failures (validation, not-found, conflict) without throwing for control
/// flow, while still throwing for truly exceptional cases (infrastructure
/// failures — see Domain.Exceptions.DomainException, still mapped centrally
/// by ExceptionHandlingMiddleware).
///
/// Deliberately collects ALL errors rather than the first one — CLAUDE.md's
/// Result Pattern requirement: "Validation failures must return ALL errors
/// ... Never stop after the first validation error." FluentValidation's
/// ValidationBehavior already does this at the request level; handlers that
/// perform additional business-rule checks after validation passes use
/// Result&lt;T&gt;.Failure(IEnumerable&lt;Error&gt;) to add to that same guarantee for
/// checks that can only run once you have DB state in hand (e.g. "recipient
/// has opted out" + "template not found" reported together).
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public IReadOnlyList<Error> Errors { get; }

    private Result(bool isSuccess, T? value, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<Error>());

    public static Result<T> Failure(Error error) => new(false, default, new[] { error });

    public static Result<T> Failure(IEnumerable<Error> errors)
    {
        var list = errors.ToList();
        if (list.Count == 0)
            throw new ArgumentException("At least one error is required for a failed result.", nameof(errors));
        return new Result<T>(false, default, list);
    }

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(Value!)) : Result<TOut>.Failure(Errors);
}

/// <summary>Non-generic variant for commands that don't return a payload (e.g. Cancel, Delete).</summary>
public sealed class Result
{
    public bool IsSuccess { get; }
    public IReadOnlyList<Error> Errors { get; }

    private Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new(true, Array.Empty<Error>());
    public static Result Failure(Error error) => new(false, new[] { error });
    public static Result Failure(IEnumerable<Error> errors)
    {
        var list = errors.ToList();
        if (list.Count == 0)
            throw new ArgumentException("At least one error is required for a failed result.", nameof(errors));
        return new Result(false, list);
    }
}
