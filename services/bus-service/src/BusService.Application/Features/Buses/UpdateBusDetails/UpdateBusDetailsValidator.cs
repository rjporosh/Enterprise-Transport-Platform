using BusService.Domain.Enums;
using FluentValidation;

namespace BusService.Application.Features.Buses.UpdateBusDetails;

public sealed class UpdateBusDetailsValidator : AbstractValidator<UpdateBusDetailsCommand>
{
    public UpdateBusDetailsValidator()
    {
        RuleFor(x => x.BusId).NotEmpty();
        RuleFor(x => x.BusType)
            .NotEmpty()
            .Must(value => Enum.TryParse<BusType>(value, ignoreCase: true, out _))
            .WithMessage($"BusType must be one of: {string.Join(", ", Enum.GetNames<BusType>())}.");
        RuleFor(x => x.TotalSeats).InclusiveBetween(1, 80);
        RuleFor(x => x.DepotId).NotEmpty();
        RuleFor(x => x.YearOfManufacture)
            .InclusiveBetween(1990, DateTime.UtcNow.Year)
            .When(x => x.YearOfManufacture.HasValue);
    }
}
