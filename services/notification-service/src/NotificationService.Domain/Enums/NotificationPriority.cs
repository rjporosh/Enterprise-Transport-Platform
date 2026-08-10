namespace NotificationService.Domain.Enums;

/// <summary>
/// Drives Quartz dispatch-job ordering and, for Sms/Push providers billed
/// per message, whether an expedited (more expensive) send path is used.
/// </summary>
public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}
