# Programmer's Guide — Validation

All write operations use **FluentValidation** validators co-located with their handlers.

## Example: RegisterBusValidator

```csharp
public sealed class RegisterBusValidator : AbstractValidator<RegisterBusCommand>
{
    public RegisterBusValidator()
    {
        RuleFor(x => x.PlateNumber)
            .NotEmpty().WithMessage("Plate number is required.")
            .MaximumLength(20).WithMessage("Plate number must not exceed 20 characters.");

        RuleFor(x => x.BusType)
            .NotEmpty().WithMessage("Bus type is required.");

        RuleFor(x => x.TotalSeats)
            .GreaterThan(0).WithMessage("Total seats must be greater than 0.")
            .LessThanOrEqualTo(120).WithMessage("Total seats cannot exceed 120.");

        RuleFor(x => x.DepotId)
            .NotEmpty().WithMessage("Depot is required.");
    }
}
```

## Example: ChangeBusStatusValidator

```csharp
public sealed class ChangeBusStatusValidator : AbstractValidator<ChangeBusStatusCommand>
{
    public ChangeBusStatusValidator()
    {
        RuleFor(x => x.NewStatus)
            .NotEmpty()
            .Must(status => Enum.TryParse<BusStatus>(status, true, out _))
            .WithMessage("Invalid bus status.");
    }
}
```

## Custom Validation

For cross-field or database-dependent validation, inject repositories or the DbContext into the validator constructor:

```csharp
public class RegisterBusValidator : AbstractValidator<RegisterBusCommand>
{
    public RegisterBusValidator(IBusDbContext dbContext)
    {
        RuleFor(x => x.PlateNumber)
            .MustAsync(async (plate, ct) => !await dbContext.Buses.AnyAsync(b => b.PlateNumber == plate, ct))
            .WithMessage("Plate number already exists.");
    }
}
```

> **Note**: Async validators require `IValidatorInterceptor` or the `AsyncValidator` base class. Keep validators simple; push complex checks to the handler if needed.
