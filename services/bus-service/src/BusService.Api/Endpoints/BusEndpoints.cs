using BusService.Application.Common.Models;
using BusService.Application.Features.Buses.ChangeBusStatus;
using BusService.Application.Features.Buses.GetBus;
using BusService.Application.Features.Buses.GetBuses;
using BusService.Application.Features.Buses.RegisterBus;
using BusService.Application.Features.Buses.UpdateBusDetails;
using BusService.Application.Features.Depots.CreateDepot;
using BusService.Application.Features.Depots.GetDepots;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BusService.Api.Endpoints;

public static class BusEndpoints
{
    public static void MapBusEndpoints(this IEndpointRouteBuilder app)
    {
        var buses = app.MapGroup("/api/v1/buses").WithTags("Buses").RequireAuthorization();

        buses.MapPost("/", RegisterBusAsync)
            .WithName("RegisterBus")
            .WithSummary("Register a new bus into the fleet.")
            .Produces<BusDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status409Conflict)
            .RequireAuthorization(policy => policy.RequireRole("Operator", "Admin"));

        buses.MapGet("/{busId:guid}", GetBusAsync)
            .WithName("GetBus")
            .WithSummary("Get a single bus by id.")
            .Produces<BusDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        buses.MapGet("/", GetBusesAsync)
            .WithName("GetBuses")
            .WithSummary("Search the fleet — filterable by operator, depot, and status.")
            .Produces<PagedResult<BusDto>>(StatusCodes.Status200OK);

        buses.MapPut("/{busId:guid}", UpdateBusDetailsAsync)
            .WithName("UpdateBusDetails")
            .WithSummary("Update a bus's type, seat count, depot, and fleet details.")
            .Produces<BusDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Operator", "Admin"));

        buses.MapPost("/{busId:guid}/status", ChangeBusStatusAsync)
            .WithName("ChangeBusStatus")
            .WithSummary("Transition a bus's status (Active / UnderMaintenance / Retired).")
            .Produces<BusDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .RequireAuthorization(policy => policy.RequireRole("Operator", "Admin"));

        var depots = app.MapGroup("/api/v1/depots").WithTags("Depots").RequireAuthorization();

        depots.MapPost("/", CreateDepotAsync)
            .WithName("CreateDepot")
            .WithSummary("Create a depot.")
            .Produces<DepotDto>(StatusCodes.Status200OK)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        depots.MapGet("/", GetDepotsAsync)
            .WithName("GetDepots")
            .WithSummary("List depots, optionally filtered by city.")
            .Produces<IReadOnlyCollection<DepotDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> RegisterBusAsync([FromBody] RegisterBusRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new RegisterBusCommand(request.OperatorId, request.PlateNumber, request.BusType, request.TotalSeats, request.DepotId, request.Manufacturer, request.Model, request.YearOfManufacture);
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetBusAsync(Guid busId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBusQuery(busId), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetBusesAsync([AsParameters] GetBusesRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var query = new GetBusesQuery(request.OperatorId, request.DepotId, request.Status, request.Page ?? 1, request.PageSize ?? 50);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateBusDetailsAsync(Guid busId, [FromBody] UpdateBusDetailsRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new UpdateBusDetailsCommand(busId, request.BusType, request.TotalSeats, request.DepotId, request.Manufacturer, request.Model, request.YearOfManufacture);
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> ChangeBusStatusAsync(Guid busId, [FromBody] ChangeBusStatusRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChangeBusStatusCommand(busId, request.NewStatus), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateDepotAsync([FromBody] CreateDepotRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateDepotCommand(request.Name, request.City, request.Address), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDepotsAsync([AsParameters] GetDepotsRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDepotsQuery(request.City), cancellationToken);
        return Results.Ok(result);
    }
}

public sealed record RegisterBusRequest(Guid OperatorId, string PlateNumber, string BusType, int TotalSeats, Guid DepotId, string? Manufacturer, string? Model, int? YearOfManufacture);
public sealed record UpdateBusDetailsRequest(string BusType, int TotalSeats, Guid DepotId, string? Manufacturer, string? Model, int? YearOfManufacture);
public sealed record ChangeBusStatusRequest(string NewStatus);
public sealed record CreateDepotRequest(string Name, string City, string? Address);
public sealed record GetBusesRequest(Guid? OperatorId, Guid? DepotId, string? Status, int? Page, int? PageSize);
public sealed record GetDepotsRequest(string? City);
