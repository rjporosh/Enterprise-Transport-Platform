using FluentValidation;

namespace NotificationService.Application.Features.Notifications.GetNotifications;

public sealed class GetNotificationsValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x)
            .Must(x => x.CreatedFromUtc is null || x.CreatedToUtc is null || x.CreatedFromUtc <= x.CreatedToUtc)
            .WithMessage("CreatedFromUtc must be earlier than or equal to CreatedToUtc.")
            .WithName("CreatedFromUtc");
    }
}
