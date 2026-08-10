using FluentValidation;

namespace RouteService.Application.Features.Schedules.SuspendSchedule;

public sealed class SuspendScheduleCommandValidator : AbstractValidator<SuspendScheduleCommand>
{
    public SuspendScheduleCommandValidator()
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
    }
}
