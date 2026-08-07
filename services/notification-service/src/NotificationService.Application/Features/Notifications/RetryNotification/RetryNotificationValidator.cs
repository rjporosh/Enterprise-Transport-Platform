using FluentValidation;

namespace NotificationService.Application.Features.Notifications.RetryNotification;

public sealed class RetryNotificationValidator : AbstractValidator<RetryNotificationCommand>
{
    public RetryNotificationValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();
        RuleFor(x => x.AdditionalAttempts).InclusiveBetween(1, 10);
    }
}
