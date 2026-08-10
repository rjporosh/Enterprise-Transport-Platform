namespace RouteService.Application.Common.Models;

public sealed record StopDto(Guid Id, string Code, string Name, string City, string? Address, double Latitude, double Longitude, string? CreatedBy, string? UpdatedBy, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
