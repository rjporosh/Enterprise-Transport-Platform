using PaymentService.Domain.Common;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Events;
using PaymentService.Domain.Exceptions;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentService.Domain.Entities;

public sealed class Payment : AggregateRoot
{
    private readonly List<PaymentRefund> _refunds = new();
    private decimal _amount;

    public Guid TenantId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string OrderReference { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? ProviderReference { get; private set; }
    public string? ProviderPaymentId { get; private set; }
    public PaymentStatus Status { get; private set; }
    public PaymentMethodType PaymentMethod { get; private set; }

    [NotMapped]
    public Money Amount => new Money(_amount, Currency);

    public string Currency { get; private set; } = "USD";
    public decimal? FeeAmount { get; private set; }
    public decimal? TaxAmount { get; private set; }
    public string? FailureReason { get; private set; }
    public string? FailureCode { get; private set; }
    public string? Metadata { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public IReadOnlyCollection<PaymentRefund> Refunds => _refunds.AsReadOnly();

    public decimal TotalRefundedAmount => _refunds
        .Where(r => r.Status != RefundStatus.Failed)
        .Sum(r => r.Amount);

    public decimal AvailableRefundAmount => Amount.Amount - TotalRefundedAmount;

    public bool IsRefundable => Status is PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded
        && AvailableRefundAmount > 0;

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAtUtc
        && Status is PaymentStatus.Pending or PaymentStatus.Processing;

    private Payment() { }

    public static Payment Create(
        Guid tenantId,
        Guid? companyId,
        Guid? organizationId,
        Guid customerId,
        string orderReference,
        string idempotencyKey,
        PaymentMethodType paymentMethod,
        Money amount,
        decimal? feeAmount,
        decimal? taxAmount,
        string? metadata,
        TimeSpan? ttl,
        DateTimeOffset? createdAtUtc = null)
    {
        ValidateTenant(tenantId);
        ValidateCustomer(customerId);
        ValidateOrderReference(orderReference);
        ValidateIdempotencyKey(idempotencyKey);
        ValidateAmount(amount);

        var now = createdAtUtc ?? DateTimeOffset.UtcNow;
        var expiresAt = now.Add(ttl ?? TimeSpan.FromMinutes(30));

        var payment = new Payment
        {
            TenantId = tenantId,
            CompanyId = companyId,
            OrganizationId = organizationId,
            CustomerId = customerId,
            OrderReference = orderReference,
            IdempotencyKey = idempotencyKey,
            PaymentMethod = paymentMethod,
            _amount = amount.Amount,
            Currency = amount.Currency,
            FeeAmount = feeAmount,
            TaxAmount = taxAmount,
            Metadata = metadata,
            Status = PaymentStatus.Pending,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        payment.Raise(new PaymentCreatedDomainEvent(
            payment.Id,
            payment.TenantId,
            payment.CompanyId,
            payment.OrganizationId,
            payment.CustomerId,
            payment.OrderReference,
            payment.Amount,
            payment.PaymentMethod,
            payment.ProviderReference,
            payment.CreatedAtUtc));

        return payment;
    }

    public void StartProcessing(string? providerReference = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidPaymentStateTransitionException(Status.ToString(), PaymentStatus.Processing.ToString());

        if (IsExpired)
            throw new InvalidOperationException("Cannot process an expired payment.");

        Status = PaymentStatus.Processing;
        ProviderReference = providerReference ?? ProviderReference;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        ProcessedAtUtc = DateTimeOffset.UtcNow;

        Raise(new PaymentProcessingDomainEvent(Id, TenantId, ProviderReference, UpdatedAtUtc.Value));
    }

    public void Succeed(string providerPaymentId, string? providerTransactionId = null)
    {
        if (Status != PaymentStatus.Processing)
            throw new InvalidPaymentStateTransitionException(Status.ToString(), PaymentStatus.Succeeded.ToString());

        Status = PaymentStatus.Succeeded;
        ProviderPaymentId = providerPaymentId;
        ProviderReference = providerTransactionId ?? ProviderReference;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        Raise(new PaymentSucceededDomainEvent(Id, TenantId, ProviderReference ?? string.Empty, ProviderPaymentId, UpdatedAtUtc.Value));
    }

    public void Fail(string reason, string? providerErrorCode = null)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            throw new InvalidPaymentStateTransitionException(Status.ToString(), PaymentStatus.Failed.ToString());

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        FailureCode = providerErrorCode;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        Raise(new PaymentFailedDomainEvent(Id, TenantId, reason, providerErrorCode, UpdatedAtUtc.Value));
    }

    public void Cancel(string? reason = null)
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
            throw new InvalidPaymentStateTransitionException(Status.ToString(), PaymentStatus.Cancelled.ToString());

        Status = PaymentStatus.Cancelled;
        FailureReason = reason ?? FailureReason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        Raise(new PaymentCancelledDomainEvent(Id, TenantId, reason, UpdatedAtUtc.Value));
    }

    public PaymentRefund InitiateRefund(decimal amount, string reason, string? initiatedByUserId = null)
    {
        if (!IsRefundable)
            throw new InvalidOperationException($"Payment {Status} cannot be refunded.");

        if (amount <= 0)
            throw new ArgumentException("Refund amount must be greater than zero.", nameof(amount));

        if (amount > AvailableRefundAmount)
            throw new InsufficientRefundAmountException(AvailableRefundAmount, amount);

        var refund = PaymentRefund.Create(
            Id,
            TenantId,
            amount,
            Currency,
            reason,
            initiatedByUserId);

        _refunds.Add(refund);

        if (TotalRefundedAmount >= Amount.Amount)
        {
            Status = PaymentStatus.Refunded;
        }
        else if (Status == PaymentStatus.Succeeded)
        {
            Status = PaymentStatus.PartiallyRefunded;
        }

        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return refund;
    }

    public void UpdateProviderReference(string? providerReference)
    {
        ProviderReference = providerReference;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static void ValidateTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
    }

    private static void ValidateCustomer(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty.", nameof(customerId));
    }

    private static void ValidateOrderReference(string orderReference)
    {
        if (string.IsNullOrWhiteSpace(orderReference))
            throw new ArgumentException("OrderReference is required.", nameof(orderReference));
    }

    private static void ValidateIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("IdempotencyKey is required.", nameof(idempotencyKey));
    }

    private static void ValidateAmount(Money amount)
    {
        if (amount.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
    }
}
