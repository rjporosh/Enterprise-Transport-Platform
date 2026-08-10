using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NotificationService.Infrastructure.Messaging;

public sealed class UserDirectoryOptions
{
    public const string SectionName = "UserDirectory";

    /// <summary>Base URL of Auth Service's internal API. The lookup endpoint this client calls does not exist yet — see IUserDirectoryClient's remarks — so until it ships, every call here fails fast and gracefully rather than hanging or throwing into the caller.</summary>
    public string? BaseUrl { get; set; }
    public int TimeoutSeconds { get; set; } = 3;
}

/// <summary>See IUserDirectoryClient for why this fails gracefully by design today. Wrapped in try/catch rather than letting HttpClient exceptions propagate — a dependency being unavailable must never take down the RabbitMQ consumer loop that calls it (CLAUDE.md: "fail gracefully and write structured logs").</summary>
public sealed class HttpUserDirectoryClient : IUserDirectoryClient
{
    private readonly HttpClient _httpClient;
    private readonly UserDirectoryOptions _options;
    private readonly ILogger<HttpUserDirectoryClient> _logger;

    public HttpUserDirectoryClient(HttpClient httpClient, IOptions<UserDirectoryOptions> options, ILogger<HttpUserDirectoryClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<UserContactInfo?> ResolveContactAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _logger.LogWarning(
                "UserDirectory:BaseUrl is not configured; cannot resolve contact info for user {UserId}. " +
                "This event will be skipped. See IUserDirectoryClient remarks.", userId);
            return null;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            var response = await _httpClient.GetAsync($"{_options.BaseUrl.TrimEnd('/')}/api/v1/users/{userId}/contact", cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "User directory lookup for {UserId} returned {StatusCode}. This endpoint is not yet implemented " +
                    "by Auth Service — see docs/architecture/notification-service-architecture.md.",
                    userId, (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UserContactInfo>(cancellationToken: cts.Token);
        }
        catch (Exception ex)
        {
            // Dependency unavailable (DNS failure, connection refused, timeout).
            // Structured runtime-error logging is configured centrally via the
            // Serilog file sink -- see Program.cs -- so this single LogError
            // call is what lands in logs/runtime-errors/.
            _logger.LogError(ex,
                "Failed to resolve contact info for user {UserId} from user directory service. " +
                "Root cause: dependency unavailable or misconfigured (UserDirectory:BaseUrl={BaseUrl}). " +
                "Possible solution: verify Auth Service is running and reachable, and that the " +
                "GET /api/v1/users/{{id}}/contact endpoint has been implemented there.",
                userId, _options.BaseUrl);
            return null;
        }
    }
}
