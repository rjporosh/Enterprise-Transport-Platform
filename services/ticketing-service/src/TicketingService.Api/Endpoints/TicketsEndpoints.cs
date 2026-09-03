using MediatR;
using TicketingService.Application.Common.Interfaces;
using TicketingService.Application.Features.Tickets;

namespace TicketingService.Api.Endpoints;

public static class TicketsEndpoints
{
    public static IEndpointRouteBuilder MapTicketsEndpoints(this IEndpointRouteBuilder app)
    {
        // Public verification — gate staff scan the QR.
        app.MapGet("/api/v1/tickets/verify/{code}", async (string code, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new VerifyTicketQuery(code), ct);
                return result is null ? Results.NotFound(new { success = false, message = "Unknown ticket code." }) : Results.Ok(result);
            })
            .WithTags("Tickets").WithName("VerifyTicket")
            .WithSummary("Public — resolve a ticket QR code to its travel details + validity.");

        var group = app.MapGroup("/api/v1/tickets").WithTags("Tickets").RequireAuthorization();

        group.MapGet("/mine", async (int? page, int? pageSize, ICurrentUser user, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyTicketsQuery(user.CustomerId ?? Guid.Empty, page ?? 1, pageSize ?? 20), ct)))
            .WithName("GetMyTickets").WithSummary("The signed-in customer's tickets.");

        group.MapGet("/{ticketId:guid}", async (Guid ticketId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            {
                var dto = await sender.Send(new GetTicketByIdQuery(ticketId, user.CustomerId ?? Guid.Empty, IsPrivileged(user)), ct);
                return dto is null ? Results.NotFound(new { success = false, message = "Ticket not found." }) : Results.Ok(dto);
            })
            .WithName("GetTicketById");

        group.MapGet("/{ticketId:guid}/pdf", async (Guid ticketId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            {
                var bytes = await sender.Send(new GetTicketPdfQuery(ticketId, user.CustomerId ?? Guid.Empty, IsPrivileged(user)), ct);
                return bytes is null
                    ? Results.NotFound(new { success = false, message = "Ticket not found." })
                    : Results.File(bytes, "application/pdf", $"ticket-{ticketId}.pdf");
            })
            .WithName("GetTicketPdf").WithSummary("Download the ticket as a PDF (print-ready).");

        group.MapPost("/{ticketId:guid}/cancel", async (Guid ticketId, CancelTicketRequest body, ICurrentUser user, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelTicketCommand(ticketId, body.Reason, user.CustomerId ?? Guid.Empty, IsPrivileged(user)), ct);
                return Results.NoContent();
            })
            .WithName("CancelTicket");

        group.MapPost("/{ticketId:guid}/reissue", async (Guid ticketId, ICurrentUser user, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new ReissueTicketCommand(ticketId, user.CustomerId ?? Guid.Empty, IsPrivileged(user)), ct);
                return Results.Ok(new { ticketId = id });
            })
            .WithName("ReissueTicket").WithSummary("Reprint — same ticket number, PDF regenerated.");

        return app;
    }

    private static bool IsPrivileged(ICurrentUser u) => u.IsInRole("Admin") || u.IsInRole("Operator");
}

public sealed record CancelTicketRequest(string Reason);
