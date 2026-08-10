namespace RouteService.Infrastructure.Communication;

public sealed class CommunicationOptions
{
    public const string SectionName = "Communication";
    public string BaseUrl { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
