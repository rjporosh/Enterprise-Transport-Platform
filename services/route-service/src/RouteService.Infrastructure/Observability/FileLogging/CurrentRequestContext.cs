namespace RouteService.Infrastructure.Observability.FileLogging;

public static class CurrentRequestContext
{
    public static string? CorrelationId { get; set; }
}
