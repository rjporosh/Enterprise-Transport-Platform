using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace PaymentService.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(ILogger<ValidationBehavior<TRequest, TResponse>> logger, IEnumerable<IValidator<TRequest>> validators)
    {
        _logger = logger;
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationTasks = _validators.Select(v => v.ValidateAsync(context, cancellationToken));
        var validationResults = await Task.WhenAll(validationTasks);

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errors = failures
            .GroupBy(f => f.PropertyName)
            .Select(g => new
            {
                Field = g.Key,
                Code = g.First().ErrorCode,
                Message = g.First().ErrorMessage
            })
            .ToList();

        foreach (var error in errors)
        {
            _logger.LogWarning(
                "Validation failed for {RequestType}. Field: {Field}, Code: {Code}, Message: {Message}",
                typeof(TRequest).Name,
                error.Field,
                error.Code,
                error.Message);
        }

        throw new FluentValidation.ValidationException(failures);
    }
}
