using FluentValidation;

namespace BusService.Application.Features.Buses.RestoreBus;

public sealed class RestoreBusValidator : AbstractValidator<RestoreBusCommand>
{
    public RestoreBusValidator()
    {
        RuleFor(x => x.BusId).NotEmpty();
    }
}
