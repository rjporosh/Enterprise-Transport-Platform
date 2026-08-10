using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Common.Models;
using PaymentService.Application.Features.AgentPaymentMethods;
using PaymentService.Domain.Enums;

namespace PaymentService.Api.Endpoints;

public static class AgentPaymentMethodEndpoints
{
    public static IEndpointRouteBuilder MapAgentPaymentMethodEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/agents/{agentId:guid}/payment-methods")
            .WithTags("AgentPaymentMethods")
            .WithOpenApi()
            .RequireAuthorization()
            .RequireRateLimiting("PaymentPolicy");

        group.MapPost("/", async (
            Guid agentId,
            AddAgentPaymentMethodCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var updatedCommand = command with { AgentId = agentId };
            var result = await sender.Send(updatedCommand, ct);
            return Results.Created($"/api/v1/agents/{agentId}/payment-methods/{result.Id}", result);
        })
        .WithName("AddAgentPaymentMethod")
        .Produces<AgentPaymentMethodDto>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/", async (
            Guid agentId,
            bool? onlyVerified,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetAgentPaymentMethodsQuery(agentId, onlyVerified, page, pageSize, ct);
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetAgentPaymentMethods")
        .Produces<PagedResult<AgentPaymentMethodDto>>(StatusCodes.Status200OK);

        group.MapGet("/default", async (
            Guid agentId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetDefaultPaymentMethodQuery(agentId, ct);
            var result = await sender.Send(query, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetDefaultAgentPaymentMethod")
        .Produces<AgentPaymentMethodDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{paymentMethodId:guid}/set-default", async (
            Guid agentId,
            Guid paymentMethodId,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new SetDefaultPaymentMethodCommand(agentId, paymentMethodId, ct);
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("SetDefaultAgentPaymentMethod")
        .Produces<AgentPaymentMethodDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{paymentMethodId:guid}/verify", async (
            Guid paymentMethodId,
            VerifyAgentPaymentMethodCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var updatedCommand = command with { PaymentMethodId = paymentMethodId };
            var result = await sender.Send(updatedCommand, ct);
            return Results.Ok(result);
        })
        .WithName("VerifyAgentPaymentMethod")
        .Produces<AgentPaymentMethodDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return endpoints;
    }
}