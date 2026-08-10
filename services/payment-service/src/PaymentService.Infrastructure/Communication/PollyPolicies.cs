using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace PaymentService.Infrastructure.Communication;

public static class PollyPolicies
{
    public static AsyncRetryPolicy GetRetryPolicy(ILogger logger) =>
        Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetryAsync: (exception, timespan, attempt, context) =>
                {
                    logger.LogWarning(
                        "Retry {Attempt} after {Delay}s due to {Reason}",
                        attempt,
                        timespan.TotalSeconds,
                        exception.Message);
                    return Task.CompletedTask;
                });

    public static AsyncTimeoutPolicy GetTimeoutPolicy(ILogger logger) =>
        Policy.TimeoutAsync(TimeSpan.FromSeconds(30), TimeoutStrategy.Pessimistic);

    public static AsyncCircuitBreakerPolicy<T> GetCircuitBreakerPolicy<T>(ILogger logger) =>
        Policy<T>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (delegateResult, breakDelay) =>
                {
                    var exception = delegateResult.Exception ?? new Exception("Circuit breaker triggered by result");
                    logger.LogCritical(exception, "Circuit breaker opened for {Delay}s", breakDelay.TotalSeconds);
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker half-open");
                });
}