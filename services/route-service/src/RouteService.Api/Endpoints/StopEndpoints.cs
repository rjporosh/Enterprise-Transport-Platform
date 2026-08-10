using RouteService.Application.Common.Models;
using RouteService.Application.Features.Stops.CreateStop;
using RouteService.Application.Features.Stops.DeleteStop;
using RouteService.Application.Features.Stops.GetStop;
using RouteService.Application.Features.Stops.GetStops;
using RouteService.Application.Features.Stops.UpdateStop;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace RouteService.Api.Endpoints;

public static class StopEndpoints
{
    public static void MapStopEndpoints(this IEndpointRouteBuilder app)
    {
        var stops = app.MapGroup("/api/v1/stops").WithTags("Stops").RequireAuthorization();

        stops.MapPost("/", CreateStopAsync)
            .WithName("CreateStop")
            .WithSummary("Create a new stop.")
            .Produces<StopDto>(StatusCodes.Status200OK)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"));

        stops.MapGet("/{stopId:guid}", GetStopAsync)
            .WithName("GetStop")
            .WithSummary("Get a stop by id.")
            .Produces<StopDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        stops.MapGet("/", GetStopsAsync)
            .WithName("GetStops")
            .WithSummary("List stops with optional city filter and pagination.")
            .Produces<PagedResult<StopDto>>(StatusCodes.Status200OK);

        stops.MapPut("/{stopId:guid}", UpdateStopAsync)
            .WithName("UpdateStop")
            .WithSummary("Update a stop.")
            .Produces<StopDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"));

        stops.MapDelete("/{stopId:guid}", DeleteStopAsync)
            .WithName("DeleteStop")
            .WithSummary("Soft-delete a stop.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private static async Task<IResult> CreateStopAsync([FromBody] CreateStopRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new CreateStopCommand(request.Code, request.Name, request.City, request.Address, request.Latitude, request.Longitude, null);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }

    private static async Task<IResult> GetStopAsync(Guid stopId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStopQuery(stopId), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetStopsAsync([AsParameters] GetStopsRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var query = new GetStopsQuery(request.City, request.SearchTerm, request.Page ?? 1, request.PageSize ?? 50);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateStopAsync(Guid stopId, [FromBody] UpdateStopRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new UpdateStopCommand(stopId, request.Name, request.City, request.Address, request.Latitude, request.Longitude, null);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }

    private static async Task<IResult> DeleteStopAsync(Guid stopId, ISender sender, CancellationToken cancellationToken)
    {
        var command = new DeleteStopCommand(stopId);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }
}

public sealed record CreateStopRequest(string Code, string Name, string City, string? Address, double Latitude, double Longitude);
public sealed record UpdateStopRequest(string Name, string City, string? Address, double Latitude, double Longitude);
public sealed record GetStopsRequest(string? City, string? SearchTerm, int? Page, int? PageSize);
