using FluentValidation;

namespace BusService.Application.Features.Depots.RestoreDepot;

public sealed class RestoreDepotValidator : AbstractValidator<RestoreDepotCommand>
{
    public RestoreDepotValidator()
    {
        RuleFor(x => x.DepotId).NotEmpty();
    }
}
