namespace BusService.Application.Common.Models;

public sealed record BusDto(
    Guid Id,
    Guid OperatorId,
    string PlateNumber,
    string BusType,
    int TotalSeats,
    Guid DepotId,
    string Status,
    string? Manufacturer,
    string? Model,
    int? YearOfManufacture,
    Guid? TenantId,
    Guid? CompanyId,
    Guid? OrganizationId,
    bool IsDeleted,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
