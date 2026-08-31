namespace Platform.SharedKernel.Idempotency;

/// <summary>
/// Contract for a distributed idempotency store. Dangerous, retryable mutations
/// (payments, bookings, notification sends, webhooks) record their outcome
/// under an <c>Idempotency-Key</c> so a retried request returns the original
/// result instead of repeating the side effect (.ai/MASTER-RULES.md §38).
///
/// The contract is defined here so every service (and the gateway) shares one
/// abstraction; the Redis-backed implementation is wired in a later milestone
/// (M9). Nothing in the platform should rely on an in-memory implementation of
/// this in a multi-instance deployment.
/// </summary>
public interface IdempotencyStore
{
    /// <summary>
    /// Atomically reserves <paramref name="key"/> for the current request when it
    /// is unseen. Returns <c>null</c> on a successful reservation (caller proceeds
    /// and then calls <see cref="CompleteAsync"/>); returns the stored record when
    /// the key was already seen (caller replays it).
    /// </summary>
    Task<IdempotencyRecord?> TryReserveAsync(
        string key,
        string requestFingerprint,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    /// <summary>Stores the final outcome for a previously reserved key.</summary>
    Task CompleteAsync(
        string key,
        int statusCode,
        string responseBody,
        CancellationToken cancellationToken = default);
}

/// <summary>A recorded idempotent outcome.</summary>
public sealed record IdempotencyRecord(
    string Key,
    string RequestFingerprint,
    IdempotencyState State,
    int? StatusCode,
    string? ResponseBody,
    DateTimeOffset CreatedAtUtc);

public enum IdempotencyState
{
    /// <summary>Reserved, request in flight — a concurrent duplicate should 409/retry-after.</summary>
    Pending = 0,

    /// <summary>Completed — the stored response should be replayed.</summary>
    Completed = 1
}
