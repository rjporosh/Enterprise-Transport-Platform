using FluentValidation;

namespace RouteService.Application.Features.Schedules.ActivateSchedule;

public sealed class ActivateScheduleCommandValidator : AbstractValidator<ActivateScheduleCommand>
{
    public ActivateScheduleCommandValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
    }
}
