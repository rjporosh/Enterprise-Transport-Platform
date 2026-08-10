namespace NotificationService.Application.Features.Release;

public sealed record ReleaseInfoDto(
    string ServiceName,
    string Version,
    string ReleaseDate,
    string ReleaseIdentifier,
    IReadOnlyList<string> NewFeatures,
    IReadOnlyList<string> ChangedFeatures,
    IReadOnlyList<string> BugFixes,
    IReadOnlyList<string> ApiChanges,
    IReadOnlyList<string> DatabaseChanges,
    IReadOnlyList<string> ConfigurationChanges,
    string TestingNotes,
    IReadOnlyList<string> BreakingChanges,
    IReadOnlyList<string> KnownLimitations);
