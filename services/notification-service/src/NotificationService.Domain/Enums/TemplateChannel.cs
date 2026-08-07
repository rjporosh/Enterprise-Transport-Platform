namespace NotificationService.Domain.Enums;

/// <summary>A template is authored for exactly one channel — subject/body shape differs too much to share (Email has Subject+Html+PlainText, Sms is a single 160/70-char body, Push has Title+Body+Data).</summary>
public enum TemplateChannel
{
    Email = 1,
    Sms = 2,
    Push = 3
}
