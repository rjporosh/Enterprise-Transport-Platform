namespace RouteService.Domain.Interfaces;

public interface IAuditable
{
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
    DateTimeOffset CreatedAtUtc { get; set; }
    DateTimeOffset UpdatedAtUtc { get; set; }
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAtUtc { get; set; }
}
