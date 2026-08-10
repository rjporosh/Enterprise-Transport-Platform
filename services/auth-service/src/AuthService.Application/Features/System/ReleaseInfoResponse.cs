namespace AuthService.Application.Features.System;

public sealed record ReleaseInfoResponse(
    string ServiceName,
    string Version,
    string ReleaseId,
    DateTimeOffset ReleaseDate,
    List<string> NewFeatures,
    List<string> ChangedFeatures,
    List<string> BugFixes,
    List<string> ApiChanges,
    List<string> DatabaseChanges,
    List<string> MigrationsRequired,
    List<string> ConfigurationChanges,
    string TestingNotes,
    List<string> BreakingChanges,
    List<string> KnownLimitations);
