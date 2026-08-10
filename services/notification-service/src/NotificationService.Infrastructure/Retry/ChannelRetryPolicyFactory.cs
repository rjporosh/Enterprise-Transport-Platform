using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace NotificationService.Infrastructure.Retry;

/// <summary>
/// Builds the Polly policy channel senders (Smtp/Sms/Fcm) wrap their
/// provider call in. This is the "in-process" retry for transient failures
/// within a single dispatch attempt (e.g. one dropped TCP connection to the
/// SMTP relay) — distinct from and complementary to Notification's own
/// RetryCount/NextRetryAtUtc state-machine retry (Domain/Entities/Notification.cs),
/// which handles the coarser-grained "this whole attempt failed, try again
/// in N minutes" case across separate NotificationDispatchJob runs.
/// </summary>
public sealed class ChannelRetryPolicyFactory
{
    private readonly RetryOptions _options;
    private readonly ILogger<ChannelRetryPolicyFactory> _logger;

    public ChannelRetryPolicyFactory(IOptions<RetryOptions> options, ILogger<ChannelRetryPolicyFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public AsyncRetryPolicy Create(string channelName) =>
        Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: Math.Max(0, _options.MaxAttempts - 1),
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(_options.BaseDelayMilliseconds * Math.Pow(2, attempt - 1)),
                onRetry: (exception, delay, attempt, _) =>
                    _logger.LogWarning(exception,
                        "{Channel} send attempt {Attempt} failed; retrying in {DelayMs}ms.",
                        channelName, attempt, delay.TotalMilliseconds));
}
