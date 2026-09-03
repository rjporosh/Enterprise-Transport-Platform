using FluentAssertions;
using TicketingService.Domain.Entities;
using TicketingService.Domain.Enums;
using TicketingService.Domain.Events;
using TicketingService.Domain.ValueObjects;
using TicketingService.Infrastructure.Pdf;
using Xunit;

namespace TicketingService.UnitTests;

public class TicketNumberTests
{
    [Fact]
    public void New_IsCheckSumValid_AndHasTheExpectedShape()
    {
        var n = TicketNumber.New(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
        n.Value.Should().StartWith("TKT-261001-");
        TicketNumber.IsValid(n.Value).Should().BeTrue();
    }

    [Fact]
    public void IsValid_RejectsAMistypedNumber()
    {
        var n = TicketNumber.New(DateTimeOffset.UtcNow).Value;
        var mistyped = n[..^3] + (n[^3] == 'A' ? 'B' : 'A') + n[^2..];
        TicketNumber.IsValid(mistyped).Should().BeFalse();
    }
}

public class TicketTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    private static Ticket Issue() => Ticket.Issue(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid(),
        "Rakib Hasan", "rakib@example.com", "+8801711002233",
        "Dhaka", "Chattogram", Now.AddDays(10), Now.AddDays(10).AddHours(6),
        "DHK-METRO-11-2345", "AC Sleeper", 2400m, "BDT",
        new[] { ("1A", "Rakib Hasan"), ("1B", "Sadia Akter") }, Now);

    [Fact]
    public void Issue_CreatesAnIssuedTicketWithNumberCodeAndSeats()
    {
        var t = Issue();
        t.Status.Should().Be(TicketStatus.Issued);
        t.Number.Should().NotBeNullOrWhiteSpace();
        t.VerificationCode.Should().NotBeNullOrWhiteSpace();
        t.Seats.Should().HaveCount(2);
        t.BuildVerificationUrl("http://gw").Should().Be($"http://gw/api/v1/tickets/verify/{t.VerificationCode}");
    }

    [Fact]
    public void RaiseIssued_EmitsTicketIssuedWithTheContactSnapshot()
    {
        var t = Issue();
        t.RaiseIssued("http://gw/pdf");
        t.DomainEvents.Should().ContainSingle(e => e is TicketIssuedDomainEvent);
        ((TicketIssuedDomainEvent)t.DomainEvents.Single()).CustomerEmail.Should().Be("rakib@example.com");
    }

    [Fact]
    public void Reissue_KeepsNumberAndCode_BumpsPrintCount_ClearsCachedPdf()
    {
        var t = Issue();
        t.AttachPdf(new byte[] { 1, 2, 3 });
        var (number, code) = (t.Number, t.VerificationCode);

        t.Reissue();

        t.Number.Should().Be(number);
        t.VerificationCode.Should().Be(code);
        t.PrintCount.Should().Be(1);
        t.PdfBytes.Should().BeNull();
        t.DomainEvents.Should().ContainSingle(e => e is TicketReissuedDomainEvent);
    }

    [Fact]
    public void Cancel_ThenReissue_Throws()
    {
        var t = Issue();
        t.Cancel("duplicate", Now);
        t.Invoking(x => x.Reissue()).Should().Throw<InvalidOperationException>();
    }
}

public class QuestPdfTicketRendererTests
{
    [Fact]
    public void Render_ProducesANonEmptyPdf()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var now = DateTimeOffset.UtcNow;
        var ticket = Ticket.Issue(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid(),
            "Rakib Hasan", "r@example.com", null, "Dhaka", "Sylhet", now.AddDays(3), now.AddDays(3).AddHours(5),
            "DHK-KA-77", "Non-AC", 700m, "BDT", new[] { ("3C", "Rakib Hasan") }, now);
        var template = TicketTemplate.Create(Guid.Empty, "Default", "Enterprise Transport", true, now);

        var bytes = new QuestPdfTicketRenderer().Render(ticket, template, "http://gw/api/v1/tickets/verify/abc");

        bytes.Should().NotBeNullOrEmpty();
        bytes.Length.Should().BeGreaterThan(1000);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }
}
