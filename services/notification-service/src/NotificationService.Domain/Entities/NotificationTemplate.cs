using NotificationService.Domain.Common;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

/// <summary>
/// A reusable, localized message template identified by a business key (e.g.
/// "booking.confirmed", "payment.failed") + locale + channel. A given key
/// typically has one row per (channel, locale) combination — SendNotification
/// resolves the exact row from (TemplateKey, Channel, Locale-with-fallback-to-English).
/// </summary>
public sealed class NotificationTemplate : AggregateRoot
{
    /// <summary>Stable business identifier, stable across locales/channels/versions — what callers (other services, Quartz jobs) reference. Not the primary key so the template can be edited/re-versioned without callers tracking a Guid.</summary>
    public string Key { get; private set; } = default!;
    public TemplateChannel Channel { get; private set; }
    public string Locale { get; private set; } = "en";
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>Email only. Supports the same {{placeholder}} syntax as Body.</summary>
    public string? Subject { get; private set; }
    /// <summary>Scriban template source. {{recipient_name}}, {{booking.reference}}, etc.</summary>
    public string Body { get; private set; } = default!;
    /// <summary>Push only: JSON template for the provider "data" payload (deep-link route, ids), rendered with the same variable set as Body.</summary>
    public string? DataPayloadTemplate { get; private set; }

    public bool IsActive { get; private set; } = true;
    public new int Version { get; private set; } = 1;

    public bool IsDeleted { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    /// <summary>EF Core row-version concurrency token — distinct from AggregateRoot.Version (business/schema version above), guards against two admins editing the same template simultaneously.</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private NotificationTemplate() { } // EF Core

    private NotificationTemplate(Guid id, string key, TemplateChannel channel, string locale, string name,
        string? description, string? subject, string body, string? dataPayloadTemplate, DateTimeOffset nowUtc) : base(id)
    {
        Key = key;
        Channel = channel;
        Locale = locale;
        Name = name;
        Description = description;
        Subject = subject;
        Body = body;
        DataPayloadTemplate = dataPayloadTemplate;
        CreatedAtUtc = nowUtc;
    }

    public static NotificationTemplate Create(
        string key, TemplateChannel channel, string locale, string name, string? description,
        string? subject, string body, string? dataPayloadTemplate, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Body is required.", nameof(body));
        if (channel == TemplateChannel.Email && string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required for Email templates.", nameof(subject));

        return new NotificationTemplate(Guid.NewGuid(), key.Trim().ToLowerInvariant(), channel,
            string.IsNullOrWhiteSpace(locale) ? "en" : locale.Trim().ToLowerInvariant(),
            name, description, subject, body, dataPayloadTemplate, nowUtc);
    }

    public void Update(string name, string? description, string? subject, string body,
        string? dataPayloadTemplate, DateTimeOffset nowUtc)
    {
        Name = name;
        Description = description;
        Subject = subject;
        Body = body;
        DataPayloadTemplate = dataPayloadTemplate;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void Activate(DateTimeOffset nowUtc) { IsActive = true; UpdatedAtUtc = nowUtc; }
    public void Deactivate(DateTimeOffset nowUtc) { IsActive = false; UpdatedAtUtc = nowUtc; }

    public void SoftDelete(DateTimeOffset nowUtc)
    {
        IsDeleted = true;
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }
}
