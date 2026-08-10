using FluentValidation;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Application.Features.Stops.DeleteStop;

public sealed class DeleteStopCommandValidator : AbstractValidator<DeleteStopCommand>
{
    public DeleteStopCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.StopId).NotEmpty();
    }
}
