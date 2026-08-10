using FluentValidation;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Application.Features.Schedules.CreateSchedule;

public sealed class CreateScheduleCommandValidator : AbstractValidator<CreateScheduleCommand>
{
    public CreateScheduleCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.DepartureTime).NotEmpty();
        RuleFor(x => x.ArrivalTime).NotEmpty();
        RuleFor(x => x.ArrivalTime).GreaterThan(x => x.DepartureTime).WithMessage(localization.GetString("Schedule.ArrivalAfterDeparture"));
        RuleFor(x => x.EffectiveFrom).LessThanOrEqualTo(x => x.EffectiveTo ?? DateTimeOffset.MaxValue);
    }
}
