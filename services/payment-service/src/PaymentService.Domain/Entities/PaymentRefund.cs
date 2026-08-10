using PaymentService.Domain.Common;
using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Entities;

public sealed class PaymentRefund : Entity
{
    public Guid PaymentId { get; private set; }
    public Guid TenantId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public string? ProviderRefundReference { get; private set; }
    public RefundStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public string? FailureCode { get; private set; }
    public string? InitiatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    private PaymentRefund() { }

    public static PaymentRefund Create(
        Guid paymentId,
        Guid tenantId,
        decimal amount,
        string currency,
        string reason,
        string? initiatedByUserId = null)
    {
        ValidateAmount(amount);

        return new PaymentRefund
        {
            PaymentId = paymentId,
            TenantId = tenantId,
            Amount = Math.Round(amount, 2),
            Currency = currency.ToUpperInvariant(),
            Reason = reason,
            Status = RefundStatus.Pending,
            InitiatedByUserId = initiatedByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void MarkProcessing()
    {
        if (Status != RefundStatus.Pending)
            throw new InvalidOperationException($"Cannot process refund in status {Status}.");

        Status = RefundStatus.Processing;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Succeed(string? providerRefundReference = null)
    {
        if (Status != RefundStatus.Processing)
            throw new InvalidOperationException($"Cannot succeed refund in status {Status}.");

        Status = RefundStatus.Succeeded;
        ProviderRefundReference = providerRefundReference;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Fail(string reason, string? failureCode = null)
    {
        if (Status != RefundStatus.Processing && Status != RefundStatus.Pending)
            throw new InvalidOperationException($"Cannot fail refund in status {Status}.");

        Status = RefundStatus.Failed;
        FailureReason = reason;
        FailureCode = failureCode;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Refund amount must be greater than zero.", nameof(amount));
    }
}
