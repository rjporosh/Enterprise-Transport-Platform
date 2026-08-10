using FluentValidation;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Application.Features.Routes.DeleteRoute;

public sealed class DeleteRouteCommandValidator : AbstractValidator<DeleteRouteCommand>
{
    public DeleteRouteCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.RouteId).NotEmpty();
    }
}
