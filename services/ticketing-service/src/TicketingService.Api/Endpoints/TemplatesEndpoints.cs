using MediatR;
using TicketingService.Application.Features.Templates;

namespace TicketingService.Api.Endpoints;

public static class TemplatesEndpoints
{
    public static IEndpointRouteBuilder MapTemplatesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ticket-templates")
            .WithTags("Ticket templates")
            .RequireAuthorization(p => p.RequireRole("Admin", "Operator"));

        group.MapGet("/", async (Guid? operatorId, int? page, int? pageSize, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetTemplatesQuery(operatorId, page ?? 1, pageSize ?? 50), ct)))
            .WithName("GetTicketTemplates");

        group.MapPost("/", async (CreateTemplateCommand command, ISender sender, CancellationToken ct) =>
            {
                var dto = await sender.Send(command, ct);
                return Results.Created($"/api/v1/ticket-templates/{dto.Id}", dto);
            })
            .WithName("CreateTicketTemplate");

        group.MapPut("/{id:guid}", async (Guid id, UpdateTemplateBody body, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new UpdateTemplateCommand(
                id, body.Name, body.BrandName, body.PrimaryColorHex, body.AccentColorHex, body.TermsText, body.FooterText, body.IsActive), ct)))
            .WithName("UpdateTicketTemplate");

        group.MapPost("/{id:guid}/logo", async (Guid id, HttpRequest request, ISender sender, CancellationToken ct) =>
            {
                if (!request.HasFormContentType) return Results.BadRequest(new { success = false, message = "multipart/form-data with a 'file' field required." });
                var form = await request.ReadFormAsync(ct);
                var file = form.Files["file"];
                if (file is null || file.Length == 0) return Results.BadRequest(new { success = false, message = "No file uploaded." });

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms, ct);
                var dto = await sender.Send(new SetTemplateLogoCommand(id, ms.ToArray()), ct);
                return Results.Ok(dto);
            })
            .WithName("SetTicketTemplateLogo")
            .WithSummary("Upload a PNG logo (≤ 512 KB) for a template.")
            .DisableAntiforgery();

        return app;
    }
}

public sealed record UpdateTemplateBody(
    string Name, string BrandName, string PrimaryColorHex, string AccentColorHex,
    string TermsText, string FooterText, bool IsActive);
