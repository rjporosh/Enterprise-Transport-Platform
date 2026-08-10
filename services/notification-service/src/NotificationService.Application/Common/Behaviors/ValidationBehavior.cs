using FluentValidation;
using MediatR;

namespace NotificationService.Application.Common.Behaviors;

/// <summary>
/// Runs all registered FluentValidation validators for a request before the
/// handler executes. FluentValidation.ValidateAsync already collects every
/// property failure (not just the first), so throwing the aggregate
/// ValidationException here — caught centrally by ExceptionHandlingMiddleware
/// and mapped to the platform's all-errors response shape — satisfies
/// CLAUDE.md's "Result Pattern... Never stop after the first validation
/// error" for the input-shape layer. Business-rule checks that need DB state
/// use Result&lt;T&gt;.Failure(IEnumerable&lt;Error&gt;) inside the handler instead.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
