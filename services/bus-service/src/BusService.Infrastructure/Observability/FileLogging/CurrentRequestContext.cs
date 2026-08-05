namespace BusService.Infrastructure.Observability.FileLogging;

/// <summary>
/// Flows "which endpoint is this database call happening on" from the HTTP
/// middleware pipeline down into the EF Core command interceptor, without
/// Infrastructure needing to reference IHttpContextAccessor / ASP.NET Core
/// types directly (would require a FrameworkReference on a plain class
/// library — this AsyncLocal avoids that entirely). AsyncLocal correctly
/// flows through async/await, which is all EF Core's async command
/// execution path needs.
/// </summary>
public static class CurrentRequestContext
{
    private static readonly AsyncLocal<string?> _endpoint = new();

    public static string? Endpoint => _endpoint.Value;

    public static void SetEndpoint(string? value) => _endpoint.Value = value;
}
