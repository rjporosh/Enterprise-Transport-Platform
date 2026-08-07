using FluentValidation;

namespace NotificationService.Application.Features.Notifications.CancelNotification;

public sealed class CancelNotificationValidator : AbstractValidator<CancelNotificationCommand>
{
    public CancelNotificationValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
