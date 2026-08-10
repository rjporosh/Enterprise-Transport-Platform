# Validation

## Framework

FluentValidation is used for all input validation.

## Registration

Validators are automatically discovered and registered in `Application/DependencyInjection.cs`:
```csharp
services.AddValidatorsFromAssembly(typeof(SomeCommand).Assembly);
```

## Behavior

`ValidationBehavior<TRequest, TResponse>` runs all validators before the handler executes. It collects ALL validation errors and throws `ValidationException` if any exist. The centralized exception handler maps these to a 400 response with all errors.

## Multi-Error Example

```csharp
public sealed class SendNotificationValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationValidator()
    {
        RuleFor(x => x.Recipient).NotEmpty().WithErrorCode("REQUIRED");
        RuleFor(x => x.Channel).IsInEnum().WithErrorCode("INVALID");
        RuleFor(x => x.Body).NotEmpty().When(x => x.TemplateKey is null).WithErrorCode("REQUIRED");
    }
}
```

If both `Recipient` and `Body` are missing, both errors are returned in the response.

## Business-Rule Validation

Business rules that require database state (e.g., "recipient opted out", "template not found") are validated inside the handler using `Result<T>.Failure(errors)`.
