using RouteService.Application.Common.Models;
using RouteService.Application.Features.Routes.CreateRoute;
using RouteService.Application.Features.Routes.DeleteRoute;
using RouteService.Application.Features.Routes.GetRoute;
using RouteService.Application.Features.Routes.GetRoutes;
using RouteService.Application.Features.Routes.SearchRoutes;
using RouteService.Application.Features.Routes.UpdateRoute;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace RouteService.Api.Endpoints;

public static class RouteEndpoints
{
    public static void MapRouteEndpoints(this IEndpointRouteBuilder app)
    {
        var routes = app.MapGroup("/api/v1/routes").WithTags("Routes").RequireAuthorization();

        routes.MapPost("/", CreateRouteAsync)
            .WithName("CreateRoute")
            .WithSummary("Create a new route.")
            .Produces<RouteDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"));

        routes.MapGet("/{routeId:guid}", GetRouteAsync)
            .WithName("GetRoute")
            .WithSummary("Get a route by id.")
            .Produces<RouteDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        routes.MapGet("/", GetRoutesAsync)
            .WithName("GetRoutes")
            .WithSummary("List routes with filtering and pagination.")
            .Produces<PagedResult<RouteDto>>(StatusCodes.Status200OK);

        routes.MapPut("/{routeId:guid}", UpdateRouteAsync)
            .WithName("UpdateRoute")
            .WithSummary("Update a route's details.")
            .Produces<RouteDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"));

        routes.MapDelete("/{routeId:guid}", DeleteRouteAsync)
            .WithName("DeleteRoute")
            .WithSummary("Soft-delete a route.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        routes.MapPost("/{routeId:guid}/restore", RestoreRouteAsync)
            .WithName("RestoreRoute")
            .WithSummary("Restore a soft-deleted route.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        routes.MapGet("/search", SearchRoutesAsync)
            .WithName("SearchRoutes")
            .WithSummary("Search routes by code or name.")
            .Produces<PagedResult<RouteDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateRouteAsync([FromBody] CreateRouteRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new CreateRouteCommand(request.Code, request.Name, request.OriginStopId, request.DestinationStopId, request.TransportMode, request.DistanceKm, request.EstimatedDuration, null);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }

    private static async Task<IResult> GetRouteAsync(Guid routeId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRouteQuery(routeId), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetRoutesAsync([AsParameters] GetRoutesRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var query = new GetRoutesQuery(request.SearchTerm, request.TransportMode, request.Status, request.Page ?? 1, request.PageSize ?? 50);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateRouteAsync(Guid routeId, [FromBody] UpdateRouteRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new UpdateRouteCommand(routeId, request.Name, request.TransportMode, request.DistanceKm, request.EstimatedDuration, request.ExpectedVersion, null);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }

    private static async Task<IResult> DeleteRouteAsync(Guid routeId, [FromQuery] uint expectedVersion, ISender sender, CancellationToken cancellationToken)
    {
        var command = new DeleteRouteCommand(routeId, expectedVersion);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }

    private static async Task<IResult> RestoreRouteAsync(Guid routeId, ISender sender, CancellationToken cancellationToken)
    {
        var command = new RouteService.Application.Features.Routes.RestoreRoute.RestoreRouteCommand(routeId);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }

    private static async Task<IResult> SearchRoutesAsync([AsParameters] SearchRoutesRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var query = new SearchRoutesQuery(request.Term, request.Page ?? 1, request.PageSize ?? 50);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }
}

public sealed record CreateRouteRequest(string Code, string Name, Guid OriginStopId, Guid DestinationStopId, string TransportMode, double DistanceKm, TimeSpan EstimatedDuration);
public sealed record UpdateRouteRequest(string Name, string TransportMode, double DistanceKm, TimeSpan EstimatedDuration, uint ExpectedVersion);
public sealed record GetRoutesRequest(string? SearchTerm, string? TransportMode, string? Status, int? Page, int? PageSize);
public sealed record SearchRoutesRequest(string Term, int? Page, int? PageSize);
