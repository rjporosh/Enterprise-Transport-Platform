using TicketingService.Domain.Common;

namespace TicketingService.Domain.Entities;

/// <summary>
/// Operator-scoped ticket layout/branding. The PDF renderer reads these
/// values — it is <b>not</b> a cloned image. <see cref="LogoPngBase64"/> is an
/// optional uploaded logo (validated PNG, size-capped). Exactly one template
/// per operator is <see cref="IsDefault"/> = the one used when a booking's
/// operator has no explicit active template; the platform ships a seeded
/// default with <see cref="OperatorId"/> = <see cref="Guid.Empty"/>.
/// </summary>
public sealed class TicketTemplate : AggregateRoot
{
    public Guid OperatorId { get; private set; }
    public string Name { get; private set; } = default!;
    public string BrandName { get; private set; } = default!;
    public string PrimaryColorHex { get; private set; } = "#1E3A8A";
    public string AccentColorHex { get; private set; } = "#F59E0B";
    public string? LogoPngBase64 { get; private set; }
    public string TermsText { get; private set; } = "Carry a valid photo ID. Ticket is non-transferable. Arrive 30 minutes before departure.";
    public string FooterText { get; private set; } = "Thank you for travelling with us.";
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private TicketTemplate() { }

    public static TicketTemplate Create(Guid operatorId, string name, string brandName, bool isDefault, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        OperatorId = operatorId,
        Name = name,
        BrandName = brandName,
        IsDefault = isDefault,
        CreatedAtUtc = now
    };

    public void Update(string name, string brandName, string primaryColorHex, string accentColorHex, string termsText, string footerText, bool isActive, DateTimeOffset now)
    {
        Name = name;
        BrandName = brandName;
        PrimaryColorHex = primaryColorHex;
        AccentColorHex = accentColorHex;
        TermsText = termsText;
        FooterText = footerText;
        IsActive = isActive;
        UpdatedAtUtc = now;
    }

    public void SetLogo(string? pngBase64, DateTimeOffset now)
    {
        LogoPngBase64 = pngBase64;
        UpdatedAtUtc = now;
    }
}
