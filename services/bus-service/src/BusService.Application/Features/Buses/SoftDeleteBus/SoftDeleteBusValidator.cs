using FluentValidation;

namespace BusService.Application.Features.Buses.SoftDeleteBus;

public sealed class SoftDeleteBusValidator : AbstractValidator<SoftDeleteBusCommand>
{
    public SoftDeleteBusValidator()
    {
        RuleFor(x => x.BusId).NotEmpty();
    }
}
