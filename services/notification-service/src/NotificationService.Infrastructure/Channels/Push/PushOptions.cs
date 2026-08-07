namespace NotificationService.Infrastructure.Channels.Push;

public sealed class PushOptions
{
    public const string SectionName = "Push";

    /// <summary>Firebase project id (from the service-account JSON's "project_id"), used to build the FCM HTTP v1 send URL.</summary>
    public string FirebaseProjectId { get; set; } = string.Empty;
    /// <summary>Path to the Firebase service-account JSON key file. Never commit this file -- see appsettings and .gitignore.</summary>
    public string ServiceAccountJsonPath { get; set; } = string.Empty;
}
