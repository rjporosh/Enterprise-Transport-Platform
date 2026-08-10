using BusService.Domain.Enums;
using FluentValidation;

namespace BusService.Application.Features.Buses.ChangeBusStatus;

public sealed class ChangeBusStatusValidator : AbstractValidator<ChangeBusStatusCommand>
{
    public ChangeBusStatusValidator()
    {
        RuleFor(x => x.BusId).NotEmpty();
        RuleFor(x => x.NewStatus)
            .NotEmpty()
            .Must(value => Enum.TryParse<BusStatus>(value, ignoreCase: true, out _))
            .WithMessage($"NewStatus must be one of: {string.Join(", ", Enum.GetNames<BusStatus>())}.");
    }
}
