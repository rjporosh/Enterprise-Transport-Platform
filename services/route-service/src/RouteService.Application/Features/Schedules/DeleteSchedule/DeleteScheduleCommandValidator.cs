using FluentValidation;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Application.Features.Schedules.DeleteSchedule;

public sealed class DeleteScheduleCommandValidator : AbstractValidator<DeleteScheduleCommand>
{
    public DeleteScheduleCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
    }
}
