using FluentValidation;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Application.Features.Schedules.UpdateSchedule;

public sealed class UpdateScheduleCommandValidator : AbstractValidator<UpdateScheduleCommand>
{
    public UpdateScheduleCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.ScheduleId).NotEmpty();
        RuleFor(x => x.DepartureTime).NotEmpty();
        RuleFor(x => x.ArrivalTime).NotEmpty();
        RuleFor(x => x.ArrivalTime).GreaterThan(x => x.DepartureTime).WithMessage(localization.GetString("Schedule.ArrivalAfterDeparture"));
    }
}
