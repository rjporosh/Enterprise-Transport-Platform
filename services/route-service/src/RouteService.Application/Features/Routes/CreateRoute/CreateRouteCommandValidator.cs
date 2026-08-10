using FluentValidation;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Application.Features.Routes.CreateRoute;

public sealed class CreateRouteCommandValidator : AbstractValidator<CreateRouteCommand>
{
    public CreateRouteCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage(localization.GetString("Route.CodeRequired"));
        RuleFor(x => x.Name).NotEmpty().WithMessage(localization.GetString("Route.NameRequired"));
        RuleFor(x => x.OriginStopId).NotEmpty();
        RuleFor(x => x.DestinationStopId).NotEmpty();
        RuleFor(x => x.DestinationStopId).NotEqual(x => x.OriginStopId).WithMessage(localization.GetString("Route.OriginDestinationSame"));
        RuleFor(x => x.DistanceKm).GreaterThan(0);
        RuleFor(x => x.EstimatedDuration).GreaterThan(TimeSpan.Zero);
    }
}
