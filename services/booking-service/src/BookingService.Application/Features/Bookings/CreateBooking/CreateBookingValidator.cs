using FluentValidation;

namespace BookingService.Application.Features.Bookings.CreateBooking;

public sealed class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("You must be signed in to book.");
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Passengers).NotEmpty().WithMessage("At least one passenger is required.");
        RuleFor(x => x.Passengers)
            .Must(p => p.Select(x => x.SeatNumber).Distinct().Count() == p.Count)
            .WithMessage("Duplicate seat numbers requested in the same booking.")
            .When(x => x.Passengers is not null);

        RuleForEach(x => x.Passengers).ChildRules(passenger =>
        {
            passenger.RuleFor(p => p.SeatNumber).NotEmpty().MaximumLength(10);
            passenger.RuleFor(p => p.FullName).NotEmpty().MaximumLength(150);
            passenger.RuleFor(p => p.Age).InclusiveBetween(1, 120);
            passenger.RuleFor(p => p.Gender).NotEmpty().MaximumLength(20);
        });
    }
}
