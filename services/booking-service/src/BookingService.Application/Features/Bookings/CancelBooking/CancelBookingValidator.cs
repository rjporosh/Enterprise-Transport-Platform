using FluentValidation;

namespace BookingService.Application.Features.Bookings.CancelBooking;

public sealed class CancelBookingValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.RequestedByCustomerId).NotEmpty().WithMessage("You must be signed in to cancel a booking.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
