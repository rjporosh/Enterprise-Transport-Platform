using PaymentService.Domain.Common;
using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Entities;

public sealed class AgentPaymentMethod : Entity
{
    public Guid AgentId { get; private set; }
    public PaymentMethodType MethodType { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string AccountNumber { get; private set; } = string.Empty;
    public string? AccountName { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsVerified { get; private set; }
    public string? VerificationToken { get; private set; }
    public string? Metadata { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private AgentPaymentMethod() { }

    public static AgentPaymentMethod Create(
        Guid agentId,
        PaymentMethodType methodType,
        string provider,
        string accountNumber,
        string? accountName,
        string? metadata = null,
        DateTimeOffset? createdAtUtc = null)
    {
        ValidateAgentId(agentId);
        ValidateProvider(provider);
        ValidateAccountNumber(accountNumber);

        var now = createdAtUtc ?? DateTimeOffset.UtcNow;

        var method = new AgentPaymentMethod
        {
            AgentId = agentId,
            MethodType = methodType,
            Provider = provider,
            AccountNumber = accountNumber,
            AccountName = accountName,
            IsDefault = false,
            IsVerified = false,
            Metadata = metadata,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        return method;
    }

    public void MarkAsDefault()
    {
        IsDefault = true;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkAsNotDefault()
    {
        IsDefault = false;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Verify(string? verificationToken = null)
    {
        IsVerified = true;
        VerificationToken = verificationToken;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void UpdateAccount(string accountNumber, string? accountName = null, string? metadata = null)
    {
        ValidateAccountNumber(accountNumber);
        AccountNumber = accountNumber;
        if (accountName is not null)
            AccountName = accountName;
        if (metadata is not null)
            Metadata = metadata;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static void ValidateAgentId(Guid agentId)
    {
        if (agentId == Guid.Empty)
            throw new ArgumentException("AgentId cannot be empty.", nameof(agentId));
    }

    private static void ValidateProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));
    }

    private static void ValidateAccountNumber(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("AccountNumber is required.", nameof(accountNumber));
    }
}