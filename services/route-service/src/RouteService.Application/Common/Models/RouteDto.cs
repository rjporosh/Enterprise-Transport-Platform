namespace RouteService.Application.Common.Models;

public sealed record RouteDto(Guid Id, string Code, string Name, Guid OriginStopId, Guid DestinationStopId, string TransportMode, double DistanceKm, TimeSpan EstimatedDuration, string Status, uint Version, string? CreatedBy, string? UpdatedBy, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
