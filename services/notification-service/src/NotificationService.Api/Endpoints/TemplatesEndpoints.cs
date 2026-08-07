using MediatR;
using NotificationService.Api.Common;
using NotificationService.Application.Features.Templates.CreateTemplate;
using NotificationService.Application.Features.Templates.DeleteTemplate;
using NotificationService.Application.Features.Templates.GetTemplateById;
using NotificationService.Application.Features.Templates.GetTemplates;
using NotificationService.Application.Features.Templates.UpdateTemplate;
using NotificationService.Domain.Enums;

namespace NotificationService.Api.Endpoints;

public static class TemplatesEndpoints
{
    public static void MapTemplatesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/templates").WithTags("Templates").RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .WithName("CreateTemplate")
            .WithSummary("Create a notification template for one channel + locale.")
            .Produces<ApiResponse<TemplateDto>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateTemplate")
            .WithSummary("Update a template's content (creates a new version).")
            .Produces<ApiResponse<TemplateDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetTemplateById")
            .Produces<ApiResponse<TemplateDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/", GetListAsync)
            .WithName("GetTemplates")
            .WithSummary("Paged, filterable, searchable template listing.")
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteTemplate")
            .WithSummary("Soft-delete a template.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateAsync(CreateTemplateCommand command, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.ToCreatedApiResult(httpContext, result.IsSuccess ? $"/api/v1/templates/{result.Value!.Id}" : string.Empty, "Template created.");
    }

    private static async Task<IResult> UpdateAsync(Guid id, UpdateTemplateRequest request, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var command = new UpdateTemplateCommand(id, request.Name, request.Description, request.Subject, request.Body, request.DataPayloadTemplate, request.IsActive);
        var result = await mediator.Send(command, cancellationToken);
        return result.ToApiResult(httpContext, "Template updated.");
    }

    private static async Task<IResult> GetByIdAsync(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTemplateByIdQuery(id), cancellationToken);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> GetListAsync(
        IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken,
        int page = 1, int pageSize = 20, TemplateChannel? channel = null, string? locale = null, bool? isActive = null, string? search = null)
    {
        var result = await mediator.Send(new GetTemplatesQuery(page, pageSize, channel, locale, isActive, search), cancellationToken);
        return result.ToApiResult(httpContext);
    }

    private static async Task<IResult> DeleteAsync(Guid id, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTemplateCommand(id), cancellationToken);
        return result.ToApiResult(httpContext, "Template deleted.");
    }

    private sealed record UpdateTemplateRequest(string Name, string? Description, string? Subject, string Body, string? DataPayloadTemplate, bool IsActive);
}
