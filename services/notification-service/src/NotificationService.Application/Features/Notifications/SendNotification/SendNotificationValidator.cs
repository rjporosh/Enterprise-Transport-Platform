using FluentValidation;

namespace NotificationService.Application.Features.Notifications.SendNotification;

public sealed class SendNotificationValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationValidator()
    {
        RuleFor(x => x.Recipient)
            .NotEmpty().WithMessage("Recipient is required.")
            .MaximumLength(320);

        RuleFor(x => x.Channel)
            .IsInEnum();

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.TemplateKey) || !string.IsNullOrWhiteSpace(x.Body))
            .WithMessage("Either TemplateKey or Body must be supplied.")
            .WithName("Body");

        RuleFor(x => x)
            .Must(x => string.IsNullOrWhiteSpace(x.TemplateKey) || string.IsNullOrWhiteSpace(x.Body))
            .WithMessage("Supply either TemplateKey or Body, not both.")
            .WithName("TemplateKey");

        RuleFor(x => x.Body)
            .MaximumLength(65536)
            .When(x => !string.IsNullOrWhiteSpace(x.Body));

        RuleFor(x => x.ScheduledForUtc)
            .Must(scheduledFor => scheduledFor is null || scheduledFor > DateTimeOffset.UtcNow.AddSeconds(-5))
            .WithMessage("ScheduledForUtc cannot be in the past.");

        RuleFor(x => x.MaxRetryCount)
            .InclusiveBetween(1, 20)
            .When(x => x.MaxRetryCount.HasValue);
    }
}
