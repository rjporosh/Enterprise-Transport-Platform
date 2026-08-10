namespace BusService.Infrastructure.Observability.FileLogging;

/// <summary>
/// Where the three file-based diagnostic logs live — see
/// docs/architecture/bus-service-architecture.md, "File-based diagnostic
/// logging" for what each one is for and why it exists alongside Serilog.
/// Defaults to a path relative to the content root that resolves correctly
/// for `dotnet run` from src/BusService.Api (two levels up = the service
/// root); override "Logging:FileLogsDirectory" with an absolute path in any
/// deployment where that relative assumption does not hold (e.g. a
/// published build run from a different working directory).
/// </summary>
public sealed class FileLoggingOptions
{
    public const string SectionName = "Logging";

    public string FileLogsDirectory { get; set; } = Path.Combine("..", "..", "logs");

    /// <summary>Query logging fires on every single database round trip — off by default outside Development to avoid the per-query I/O overhead in production unless explicitly opted into.</summary>
    public bool EnableQueryLogging { get; set; }
}
