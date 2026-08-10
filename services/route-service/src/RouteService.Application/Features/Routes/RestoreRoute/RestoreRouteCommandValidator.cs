using FluentValidation;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Application.Features.Routes.RestoreRoute;

public sealed class RestoreRouteCommandValidator : AbstractValidator<RestoreRouteCommand>
{
    public RestoreRouteCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.RouteId).NotEmpty();
    }
}
