using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TicketingService.Application.Common.Interfaces;
using TicketingService.Domain.Entities;

namespace TicketingService.Infrastructure.Pdf;

/// <summary>
/// Renders a ticket to an A5 PDF with QuestPDF (Community licence, set in
/// Program.cs). Template-driven layout — brand name, colours, logo, terms —
/// with a QR to the verification URL. Not a cloned image.
/// </summary>
public sealed class QuestPdfTicketRenderer : ITicketPdfRenderer
{
    public byte[] Render(Ticket ticket, TicketTemplate template, string verificationUrl)
    {
        var primary = ParseColor(template.PrimaryColorHex, Colors.Blue.Darken3);
        var accent = ParseColor(template.AccentColorHex, Colors.Amber.Medium);
        var qrPng = QrPng(verificationUrl);
        byte[]? logo = TryDecode(template.LogoPngBase64);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(24);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        if (logo is not null) col.Item().Height(28).Image(logo).FitHeight();
                        col.Item().Text(template.BrandName).FontSize(16).Bold().FontColor(primary);
                        col.Item().Text("E-Ticket / Boarding Pass").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                    row.ConstantItem(90).Image(qrPng);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(6);
                    col.Item().LineHorizontal(1).LineColor(accent);

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FROM").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().Text(ticket.OriginCity).FontSize(14).Bold();
                        });
                        r.ConstantItem(30).AlignCenter().Text("→").FontSize(14).FontColor(accent);
                        r.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("TO").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().Text(ticket.DestinationCity).FontSize(14).Bold();
                        });
                    });

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Departs: ").SemiBold(); t.Span($"{ticket.DepartureUtc:ddd, dd MMM yyyy HH:mm} UTC"); });
                        r.RelativeItem().AlignRight().Text(t => { t.Span("Arrives: ").SemiBold(); t.Span($"{ticket.ArrivalUtc:HH:mm} UTC"); });
                    });

                    col.Item().PaddingTop(4).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text(t => { t.Span("Ticket No: ").SemiBold(); t.Span(ticket.Number); });
                            r.RelativeItem().AlignRight().Text(t => { t.Span("Bus: ").SemiBold(); t.Span($"{ticket.BusPlateNumber} ({ticket.BusType})"); });
                        });
                        c.Item().Text(t =>
                        {
                            t.Span("Seats: ").SemiBold();
                            t.Span(string.Join(", ", ticket.Seats.Select(s => $"{s.SeatNumber} ({s.PassengerName})")));
                        });
                        c.Item().Text(t => { t.Span("Passenger: ").SemiBold(); t.Span(ticket.CustomerName); });
                        c.Item().Text(t => { t.Span("Fare paid: ").SemiBold(); t.Span($"{ticket.TotalAmount:0.00} {ticket.Currency}"); });
                        c.Item().Text(t => { t.Span("Status: ").SemiBold(); t.Span(ticket.Status.ToString()); });
                    });

                    col.Item().PaddingTop(6).Text(template.TermsText).FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    col.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text(template.FooterText).FontSize(8).FontColor(Colors.Grey.Medium);
                        r.RelativeItem().AlignRight().Text($"Verify: {verificationUrl}").FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static byte[] QrPng(string text)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(8);
    }

    private static byte[]? TryDecode(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return null;
        try { return Convert.FromBase64String(base64); } catch { return null; }
    }

    private static Color ParseColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try { return Color.FromHex(hex); } catch { return fallback; }
    }
}
