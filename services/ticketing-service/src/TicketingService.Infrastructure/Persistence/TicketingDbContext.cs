using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingService.Application.Common.Interfaces;
using TicketingService.Domain.Entities;
using TicketingService.Infrastructure.Persistence.Inbox;
using TicketingService.Infrastructure.Persistence.Outbox;

namespace TicketingService.Infrastructure.Persistence;

public sealed class TicketingDbContext : DbContext, ITicketingDbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options) : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketTemplate> TicketTemplates => Set<TicketTemplate>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("ticketing");

        b.Entity<Ticket>(e =>
        {
            e.ToTable("tickets");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Number).IsUnique();
            e.HasIndex(x => x.VerificationCode).IsUnique();
            e.HasIndex(x => x.BookingId).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.Property(x => x.Number).HasMaxLength(40).IsRequired();
            e.Property(x => x.VerificationCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
            e.Property(x => x.CustomerEmail).HasMaxLength(256).IsRequired();
            e.Property(x => x.CustomerPhone).HasMaxLength(32);
            e.Property(x => x.OriginCity).HasMaxLength(100).IsRequired();
            e.Property(x => x.DestinationCity).HasMaxLength(100).IsRequired();
            e.Property(x => x.BusPlateNumber).HasMaxLength(32).IsRequired();
            e.Property(x => x.BusType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.TotalAmount).HasColumnType("decimal(10,2)");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PdfBytes).HasColumnType("bytea");
            Concurrency(e);
            e.HasMany(x => x.Seats).WithOne().HasForeignKey(s => s.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(x => x.Seats).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Ignore(x => x.DomainEvents);
        });

        b.Entity<TicketSeat>(e =>
        {
            e.ToTable("ticket_seats");
            e.HasKey(x => x.Id);
            e.Property(x => x.SeatNumber).HasMaxLength(10).IsRequired();
            e.Property(x => x.PassengerName).HasMaxLength(150).IsRequired();
        });

        b.Entity<TicketTemplate>(e =>
        {
            e.ToTable("ticket_templates");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OperatorId, x.IsDefault });
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.BrandName).HasMaxLength(100).IsRequired();
            e.Property(x => x.PrimaryColorHex).HasMaxLength(9);
            e.Property(x => x.AccentColorHex).HasMaxLength(9);
            e.Property(x => x.TermsText).HasMaxLength(2000);
            e.Property(x => x.FooterText).HasMaxLength(500);
            e.Property(x => x.LogoPngBase64).HasColumnType("text");
            Concurrency(e);
            e.Ignore(x => x.DomainEvents);
        });

        b.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(300).IsRequired();
            e.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
            e.Property(x => x.Error).HasMaxLength(2000);
            e.HasIndex(x => new { x.ProcessedOnUtc, x.OccurredOnUtc });
        });

        b.Entity<InboxMessage>(e =>
        {
            e.ToTable("inbox_messages");
            e.HasKey(x => new { x.Id, x.Consumer });
            e.Property(x => x.Consumer).HasMaxLength(100).IsRequired();
            e.Property(x => x.RoutingKey).HasMaxLength(100).IsRequired();
        });

        base.OnModelCreating(b);
    }

    private static void Concurrency<T>(EntityTypeBuilder<T> e) where T : Domain.Common.AggregateRoot =>
        e.Property(x => x.Version).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsRowVersion();
}
