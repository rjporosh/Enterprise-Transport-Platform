namespace NotificationService.Application.Common.Interfaces;

/// <summary>Abstraction over DateTimeOffset.UtcNow so handlers/entities are unit-testable with a fixed clock (see TestSupport/FakeDateTimeProvider).</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
