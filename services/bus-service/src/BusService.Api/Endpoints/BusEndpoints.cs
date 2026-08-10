using BusService.Application.Common.Models;
using BusService.Application.Features.Buses.ChangeBusStatus;
using BusService.Application.Features.Buses.GetBus;
using BusService.Application.Features.Buses.GetBuses;
using BusService.Application.Features.Buses.RegisterBus;
using BusService.Application.Features.Buses.RestoreBus;
using BusService.Application.Features.Buses.SoftDeleteBus;
using BusService.Application.Features.Buses.UpdateBusDetails;
using BusService.Application.Features.Depots.CreateDepot;
using BusService.Application.Features.Depots.GetDepots;
using BusService.Application.Features.Depots.RestoreDepot;
using BusService.Application.Features.Depots.SoftDeleteDepot;
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
            .Produces<Result<BusDto>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .Produces<Result>(StatusCodes.Status409Conflict)
            .RequireAuthorization(policy => policy.RequireRole("Operator", "Admin"))
            .RequireRateLimiting("bus-write");

        buses.MapGet("/{busId:guid}", GetBusAsync)
            .WithName("GetBus")
            .WithSummary("Get a single bus by id.")
            .Produces<Result<BusDto>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .RequireRateLimiting("bus-read");

        buses.MapGet("/", GetBusesAsync)
            .WithName("GetBuses")
            .WithSummary("Search the fleet — filterable by operator, depot, and status.")
            .Produces<Result<PagedResult<BusDto>>>(StatusCodes.Status200OK)
            .RequireRateLimiting("bus-read");

        buses.MapPut("/{busId:guid}", UpdateBusDetailsAsync)
            .WithName("UpdateBusDetails")
            .WithSummary("Update a bus's type, seat count, depot, and fleet details.")
            .Produces<Result<BusDto>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Operator", "Admin"))
            .RequireRateLimiting("bus-write");

        buses.MapPost("/{busId:guid}/status", ChangeBusStatusAsync)
            .WithName("ChangeBusStatus")
            .WithSummary("Transition a bus's status (Active / UnderMaintenance / Retired).")
            .Produces<Result<BusDto>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(policy => policy.RequireRole("Operator", "Admin"))
            .RequireRateLimiting("bus-write");

        buses.MapDelete("/{busId:guid}", SoftDeleteBusAsync)
            .WithName("SoftDeleteBus")
            .WithSummary("Soft delete a bus.")
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Operator", "Admin"))
            .RequireRateLimiting("bus-write");

        buses.MapPost("/{busId:guid}/restore", RestoreBusAsync)
            .WithName("RestoreBus")
            .WithSummary("Restore a soft-deleted bus.")
            .Produces<Result<BusDto>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Operator", "Admin"))
            .RequireRateLimiting("bus-write");

        var depots = app.MapGroup("/api/v1/depots").WithTags("Depots").RequireAuthorization();

        depots.MapPost("/", CreateDepotAsync)
            .WithName("CreateDepot")
            .WithSummary("Create a depot.")
            .Produces<Result<DepotDto>>(StatusCodes.Status200OK)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .RequireRateLimiting("bus-write");

        depots.MapGet("/", GetDepotsAsync)
            .WithName("GetDepots")
            .WithSummary("List depots, optionally filtered by city.")
            .Produces<Result<IReadOnlyCollection<DepotDto>>>(StatusCodes.Status200OK)
            .RequireRateLimiting("bus-read");

        depots.MapDelete("/{depotId:guid}", SoftDeleteDepotAsync)
            .WithName("SoftDeleteDepot")
            .WithSummary("Soft delete a depot.")
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .RequireRateLimiting("bus-write");

        depots.MapPost("/{depotId:guid}/restore", RestoreDepotAsync)
            .WithName("RestoreDepot")
            .WithSummary("Restore a soft-deleted depot.")
            .Produces<Result<DepotDto>>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .RequireRateLimiting("bus-write");

        app.MapGet("/api/v1/release-info", GetReleaseInfoAsync)
            .WithName("GetReleaseInfo")
            .WithSummary("Get service release information for SQA/testers.")
            .Produces<Result<ReleaseInfoDto>>(StatusCodes.Status200OK)
            .AllowAnonymous();
    }

    private static async Task<IResult> RegisterBusAsync([FromBody] RegisterBusRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new RegisterBusCommand(request.OperatorId, request.PlateNumber, request.BusType, request.TotalSeats, request.DepotId, request.Manufacturer, request.Model, request.YearOfManufacture, request.TenantId, request.CompanyId, request.OrganizationId);
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(Result<BusDto>.SuccessResult(result, "Bus registered successfully."));
    }

    private static async Task<IResult> GetBusAsync(Guid busId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBusQuery(busId), cancellationToken);
        return Results.Ok(Result<BusDto>.SuccessResult(result));
    }

    private static async Task<IResult> GetBusesAsync([AsParameters] GetBusesRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var query = new GetBusesQuery(request.OperatorId, request.DepotId, request.TenantId, request.CompanyId, request.OrganizationId, request.Status, request.Page ?? 1, request.PageSize ?? 50);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(Result<PagedResult<BusDto>>.SuccessResult(result));
    }

    private static async Task<IResult> UpdateBusDetailsAsync(Guid busId, [FromBody] UpdateBusDetailsRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new UpdateBusDetailsCommand(busId, request.BusType, request.TotalSeats, request.DepotId, request.Manufacturer, request.Model, request.YearOfManufacture);
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(Result<BusDto>.SuccessResult(result, "Bus details updated successfully."));
    }

    private static async Task<IResult> ChangeBusStatusAsync(Guid busId, [FromBody] ChangeBusStatusRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChangeBusStatusCommand(busId, request.NewStatus), cancellationToken);
        return Results.Ok(Result<BusDto>.SuccessResult(result, $"Bus status changed to {request.NewStatus}."));
    }

    private static async Task<IResult> SoftDeleteBusAsync(Guid busId, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new SoftDeleteBusCommand(busId), cancellationToken);
        return Results.Ok(Result.SuccessResult("Bus soft-deleted successfully."));
    }

    private static async Task<IResult> RestoreBusAsync(Guid busId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RestoreBusCommand(busId), cancellationToken);
        return Results.Ok(Result<BusDto>.SuccessResult(result, "Bus restored successfully."));
    }

    private static async Task<IResult> CreateDepotAsync([FromBody] CreateDepotRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new CreateDepotCommand(request.Name, request.City, request.Address, request.TenantId, request.CompanyId, request.OrganizationId);
        var result = await sender.Send(command, cancellationToken);
        return Results.Ok(Result<DepotDto>.SuccessResult(result, "Depot created successfully."));
    }

    private static async Task<IResult> GetDepotsAsync([AsParameters] GetDepotsRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var query = new GetDepotsQuery(request.City, request.TenantId);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(Result<IReadOnlyCollection<DepotDto>>.SuccessResult(result));
    }

    private static async Task<IResult> SoftDeleteDepotAsync(Guid depotId, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new SoftDeleteDepotCommand(depotId), cancellationToken);
        return Results.Ok(Result.SuccessResult("Depot soft-deleted successfully."));
    }

    private static async Task<IResult> RestoreDepotAsync(Guid depotId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RestoreDepotCommand(depotId), cancellationToken);
        return Results.Ok(Result<DepotDto>.SuccessResult(result, "Depot restored successfully."));
    }

    private static IResult GetReleaseInfoAsync()
    {
        var info = new ReleaseInfoDto(
            ServiceName: "Bus Service",
            Version: "1.0.0",
            Environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            BuildNumber: GetBuildNumber(),
            BuildDate: GetBuildDate(),
            CommitHash: GetCommitHash(),
            Features: new[]
            {
                "Bus CRUD",
                "Fleet management",
                "Bus type management",
                "Seat configuration",
                "Status lifecycle",
                "Search/filter/pagination",
                "Soft delete",
                "Optimistic concurrency",
                "SaaS isolation",
                "RabbitMQ event-driven",
                "Redis caching",
                "OpenTelemetry",
                "Health checks",
                "gRPC",
                "Rate limiting",
                "Idempotency",
                "Audit logging"
            });

        return Results.Ok(Result<ReleaseInfoDto>.SuccessResult(info));
    }

    private static string GetBuildNumber() => Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "local";

    private static string GetBuildDate() => Environment.GetEnvironmentVariable("BUILD_DATE") ?? File.GetLastWriteTimeUtc(typeof(BusEndpoints).Assembly.Location).ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    private static string GetCommitHash() => Environment.GetEnvironmentVariable("COMMIT_HASH") ?? "dev";
}

public sealed record RegisterBusRequest(Guid OperatorId, string PlateNumber, string BusType, int TotalSeats, Guid DepotId, string? Manufacturer, string? Model, int? YearOfManufacture, Guid? TenantId, Guid? CompanyId, Guid? OrganizationId);
public sealed record UpdateBusDetailsRequest(string BusType, int TotalSeats, Guid DepotId, string? Manufacturer, string? Model, int? YearOfManufacture);
public sealed record ChangeBusStatusRequest(string NewStatus);
public sealed record CreateDepotRequest(string Name, string City, string? Address, Guid? TenantId, Guid? CompanyId, Guid? OrganizationId);
public sealed record GetBusesRequest(Guid? OperatorId, Guid? DepotId, Guid? TenantId, Guid? CompanyId, Guid? OrganizationId, string? Status, int? Page, int? PageSize);
public sealed record GetDepotsRequest(string? City, Guid? TenantId);
public sealed record ReleaseInfoDto(string ServiceName, string Version, string Environment, string BuildNumber, string BuildDate, string CommitHash, string[] Features);
