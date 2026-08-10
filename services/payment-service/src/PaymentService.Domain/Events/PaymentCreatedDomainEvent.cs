namespace PaymentService.Domain.Events;

using PaymentService.Domain.Common;
using PaymentService.Domain.Enums;

public sealed record PaymentCreatedDomainEvent(
    Guid PaymentId,
    Guid TenantId,
    Guid? CompanyId,
    Guid? OrganizationId,
    Guid CustomerId,
    string OrderReference,
    Money Amount,
    PaymentMethodType PaymentMethod,
    string? ProviderReference,
    DateTimeOffset OccurredOnUtc) : DomainEvent(Guid.NewGuid(), OccurredOnUtc);
