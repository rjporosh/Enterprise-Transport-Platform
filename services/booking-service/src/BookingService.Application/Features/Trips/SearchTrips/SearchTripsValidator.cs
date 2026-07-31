using FluentValidation;

namespace BookingService.Application.Features.Trips.SearchTrips;

public sealed class SearchTripsValidator : AbstractValidator<SearchTripsQuery>
{
    public SearchTripsValidator()
    {
        RuleFor(x => x.OriginCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DestinationCity).NotEmpty().MaximumLength(100)
            .NotEqual(x => x.OriginCity).WithMessage("Origin and destination must differ.");
        RuleFor(x => x.DepartureDate).GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Departure date cannot be in the past.");
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
