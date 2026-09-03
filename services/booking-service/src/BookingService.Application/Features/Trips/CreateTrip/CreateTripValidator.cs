using FluentValidation;

namespace BookingService.Application.Features.Trips.CreateTrip;

public sealed class CreateTripValidator : AbstractValidator<CreateTripCommand>
{
    public CreateTripValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.BusId).NotEmpty();
        RuleFor(x => x.OriginCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DestinationCity).NotEmpty().MaximumLength(100)
            .NotEqual(x => x.OriginCity).WithMessage("Origin and destination must differ.");
        RuleFor(x => x.DistanceKm).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BusPlateNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BusType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TotalSeats).InclusiveBetween(1, 120)
            .When(x => x.SeatMap is null or { Count: 0 });
        RuleFor(x => x.DepartureUtc).GreaterThan(DateTimeOffset.UtcNow).WithMessage("Departure must be in the future.");
        RuleFor(x => x.ArrivalUtc).GreaterThan(x => x.DepartureUtc).WithMessage("Arrival must be after departure.");
        RuleFor(x => x.BasePrice).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
