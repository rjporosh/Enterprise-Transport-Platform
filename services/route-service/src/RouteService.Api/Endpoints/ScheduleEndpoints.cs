using RouteService.Application.Common.Models;
using RouteService.Application.Features.Schedules.ActivateSchedule;
using RouteService.Application.Features.Schedules.CreateSchedule;
using RouteService.Application.Features.Schedules.DeleteSchedule;
using RouteService.Application.Features.Schedules.GetSchedule;
using RouteService.Application.Features.Schedules.GetSchedules;
using RouteService.Application.Features.Schedules.SuspendSchedule;
using RouteService.Application.Features.Schedules.UpdateSchedule;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace RouteService.Api.Endpoints;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var schedules = app.MapGroup("/api/v1/schedules").WithTags("Schedules").RequireAuthorization();

        schedules.MapPost("/", CreateScheduleAsync)
            .WithName("CreateSchedule")
            .WithSummary("Create a new schedule.")
            .Produces<ScheduleDto>(StatusCodes.Status200OK)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"));

        schedules.MapGet("/{scheduleId:guid}", GetScheduleAsync)
            .WithName("GetSchedule")
            .WithSummary("Get a schedule by id.")
            .Produces<ScheduleDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        schedules.MapGet("/", GetSchedulesAsync)
            .WithName("GetSchedules")
            .WithSummary("List schedules with optional route filter and pagination.")
            .Produces<PagedResult<ScheduleDto>>(StatusCodes.Status200OK);

        schedules.MapPut("/{scheduleId:guid}", UpdateScheduleAsync)
            .WithName("UpdateSchedule")
            .WithSummary("Update a schedule.")
            .Produces<ScheduleDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"));

        schedules.MapDelete("/{scheduleId:guid}", DeleteScheduleAsync)
            .WithName("DeleteSchedule")
            .WithSummary("Soft-delete a schedule.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        schedules.MapPost("/{scheduleId:guid}/activate", ActivateScheduleAsync)
            .WithName("ActivateSchedule")
            .WithSummary("Activate a planned schedule.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"));

        schedules.MapPost("/{scheduleId:guid}/suspend", SuspendScheduleAsync)
            .WithName("SuspendSchedule")
            .WithSummary("Suspend an active schedule.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"));
    }

    private static async Task<IResult> CreateScheduleAsync([FromBody] CreateScheduleRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new CreateScheduleCommand(request.RouteId, request.DepartureTime, request.ArrivalTime, request.EffectiveFrom, request.EffectiveTo, null);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }

    private static async Task<IResult> GetScheduleAsync(Guid scheduleId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetScheduleQuery(scheduleId), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetSchedulesAsync([AsParameters] GetSchedulesRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var query = new GetSchedulesQuery(request.RouteId, request.Status, request.Page ?? 1, request.PageSize ?? 50);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateScheduleAsync(Guid scheduleId, [FromBody] UpdateScheduleRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var command = new UpdateScheduleCommand(scheduleId, request.DepartureTime, request.ArrivalTime, request.EffectiveTo, request.ExpectedVersion, null);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }

    private static async Task<IResult> DeleteScheduleAsync(Guid scheduleId, [FromQuery] uint expectedVersion, ISender sender, CancellationToken cancellationToken)
    {
        var command = new DeleteScheduleCommand(scheduleId, expectedVersion);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }

    private static async Task<IResult> ActivateScheduleAsync(Guid scheduleId, ISender sender, CancellationToken cancellationToken)
    {
        var command = new ActivateScheduleCommand(scheduleId);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }

    private static async Task<IResult> SuspendScheduleAsync(Guid scheduleId, ISender sender, CancellationToken cancellationToken)
    {
        var command = new SuspendScheduleCommand(scheduleId);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Message }));
    }
}

public sealed record CreateScheduleRequest(Guid RouteId, TimeSpan DepartureTime, TimeSpan ArrivalTime, DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo);
public sealed record UpdateScheduleRequest(TimeSpan DepartureTime, TimeSpan ArrivalTime, DateTimeOffset? EffectiveTo, uint ExpectedVersion);
public sealed record GetSchedulesRequest(Guid? RouteId, string? Status, int? Page, int? PageSize);
