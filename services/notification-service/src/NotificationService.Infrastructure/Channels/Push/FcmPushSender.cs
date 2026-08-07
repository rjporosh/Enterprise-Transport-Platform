using System.Net.Http.Headers;
using System.Net.Http.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Infrastructure.Retry;

namespace NotificationService.Infrastructure.Channels.Push;

/// <summary>
/// Sends push notifications via Firebase Cloud Messaging's HTTP v1 API
/// (the current, non-deprecated FCM API — the older "legacy" server-key API
/// is being sunset by Google). HTTP v1 requires a short-lived OAuth2 access
/// token minted from a service-account JSON key rather than a static server
/// key; GoogleCredential handles that token fetch/cache/refresh so this
/// class doesn't hand-roll JWT signing.
/// https://firebase.google.com/docs/cloud-messaging/migrate-v1
/// </summary>
public sealed class FcmPushSender : IPushSender
{
    private static readonly string[] Scopes = { "https://www.googleapis.com/auth/firebase.messaging" };

    private readonly HttpClient _httpClient;
    private readonly PushOptions _options;
    private readonly ChannelRetryPolicyFactory _retryPolicyFactory;
    private readonly ILogger<FcmPushSender> _logger;
    private GoogleCredential? _credential;

    public FcmPushSender(HttpClient httpClient, IOptions<PushOptions> options, ChannelRetryPolicyFactory retryPolicyFactory, ILogger<FcmPushSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _retryPolicyFactory = retryPolicyFactory;
        _logger = logger;
    }

    public async Task<ChannelSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.FirebaseProjectId) || string.IsNullOrWhiteSpace(_options.ServiceAccountJsonPath))
        {
            _logger.LogError(
                "Push:FirebaseProjectId or Push:ServiceAccountJsonPath is not configured. " +
                "Root cause: missing configuration. Possible solution: set both in appsettings (or user-secrets/ " +
                "environment variables in production) and provide the Firebase service-account JSON key file.");
            return new ChannelSendResult(false, null, "FCM is not configured.");
        }

        var policy = _retryPolicyFactory.Create("Push(FCM)");

        try
        {
            var messageName = await policy.ExecuteAsync(async () =>
            {
                var accessToken = await GetAccessTokenAsync(cancellationToken);

                var url = $"https://fcm.googleapis.com/v1/projects/{_options.FirebaseProjectId}/messages:send";
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                request.Content = JsonContent.Create(new
                {
                    message = new
                    {
                        token = message.DeviceToken,
                        notification = new { title = message.Title, body = message.Body },
                        data = message.Data
                    }
                });

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"FCM returned {(int)response.StatusCode}: {body}");

                using var json = System.Text.Json.JsonDocument.Parse(body);
                return json.RootElement.TryGetProperty("name", out var name) ? name.GetString() : null;
            });

            return new ChannelSendResult(true, messageName, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Push send to device token ending '...{TokenTail}' failed after retries. Root cause: FCM rejected " +
                "the request (invalid/expired device token, malformed payload) or is unreachable. Possible " +
                "solution: verify the service-account key is valid and Firebase Cloud Messaging API is enabled " +
                "for project '{ProjectId}'; if the error is UNREGISTERED, the client app'\''s token is stale and " +
                "should be re-registered on next app launch.",
                message.DeviceToken.Length > 6 ? message.DeviceToken[^6..] : message.DeviceToken, _options.FirebaseProjectId);
            return new ChannelSendResult(false, null, ex.Message);
        }
    }

    /// <summary>GoogleCredential caches and auto-refreshes the token internally; re-created only if the key file changes on disk (not watched here — a service restart picks up a rotated key, which matches how every other provider secret in this service is rotated).</summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        _credential ??= GoogleCredential.FromFile(_options.ServiceAccountJsonPath).CreateScoped(Scopes);
        return await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
    }
}
