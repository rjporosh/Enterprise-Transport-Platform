namespace BookingService.Infrastructure.Observability.FileLogging;

/// <summary>
/// Flows "which endpoint / handler is this database call happening on" from
/// the HTTP pipeline down into the EF Core command interceptor without
/// Infrastructure referencing ASP.NET Core types. <see cref="AsyncLocal{T}"/>
/// correctly flows through async/await, which is all EF Core's async command
/// path needs. Also carries the correlation id so every logged query is
/// traceable back to the request that issued it.
/// </summary>
public static class CurrentRequestContext
{
    private static readonly AsyncLocal<string?> _endpoint = new();
    private static readonly AsyncLocal<string?> _handler = new();
    private static readonly AsyncLocal<string?> _correlationId = new();

    public static string? Endpoint => _endpoint.Value;
    public static string? Handler => _handler.Value;
    public static string? CorrelationId => _correlationId.Value;

    public static void SetEndpoint(string? value) => _endpoint.Value = value;
    public static void SetHandler(string? value) => _handler.Value = value;
    public static void SetCorrelationId(string? value) => _correlationId.Value = value;
}
