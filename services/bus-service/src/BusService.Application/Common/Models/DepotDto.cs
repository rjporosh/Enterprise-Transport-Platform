namespace BusService.Application.Common.Models;

public sealed record DepotDto(Guid Id, string Name, string City, string? Address, Guid? TenantId, Guid? CompanyId, Guid? OrganizationId, bool IsDeleted);
