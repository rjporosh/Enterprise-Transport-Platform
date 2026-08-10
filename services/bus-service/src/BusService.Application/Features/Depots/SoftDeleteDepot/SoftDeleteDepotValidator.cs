using FluentValidation;

namespace BusService.Application.Features.Depots.SoftDeleteDepot;

public sealed class SoftDeleteDepotValidator : AbstractValidator<SoftDeleteDepotCommand>
{
    public SoftDeleteDepotValidator()
    {
        RuleFor(x => x.DepotId).NotEmpty();
    }
}
