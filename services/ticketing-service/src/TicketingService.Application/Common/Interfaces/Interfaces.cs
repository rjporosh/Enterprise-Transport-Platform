using Microsoft.EntityFrameworkCore;
using TicketingService.Domain.Common;
using TicketingService.Domain.Entities;

namespace TicketingService.Application.Common.Interfaces;

public interface ITicketingDbContext
{
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketTemplate> TicketTemplates { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IEventPublisher
{
    Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? CustomerId { get; }
    bool IsInRole(string role);
}

/// <summary>Renders a ticket to a PDF (QuestPDF), embedding a QR to the verify URL.</summary>
public interface ITicketPdfRenderer
{
    byte[] Render(Ticket ticket, TicketTemplate template, string verificationUrl);
}

/// <summary>Ticketing settings the Application layer needs (bound in Infrastructure DI).</summary>
public sealed class TicketingSettings
{
    /// <summary>Public origin the verification QR + PDF links resolve against (the gateway).</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:8088";
}
