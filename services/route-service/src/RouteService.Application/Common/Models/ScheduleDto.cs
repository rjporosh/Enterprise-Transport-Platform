namespace RouteService.Application.Common.Models;

public sealed record ScheduleDto(Guid Id, Guid RouteId, TimeSpan DepartureTime, TimeSpan ArrivalTime, string Status, DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo, uint Version, string? CreatedBy, string? UpdatedBy, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
