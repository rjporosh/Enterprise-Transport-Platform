using FluentValidation;

namespace BusService.Application.Features.Depots.CreateDepot;

public sealed class CreateDepotValidator : AbstractValidator<CreateDepotCommand>
{
    public CreateDepotValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).MaximumLength(300).When(x => x.Address is not null);
    }
}
