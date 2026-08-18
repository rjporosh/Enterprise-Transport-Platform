using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Common.Models;
using PaymentService.Application.Features.Payments;
using PaymentService.Application.Features.Payments.CancelPayment;
using PaymentService.Application.Features.Payments.ConfirmPayment;
using PaymentService.Application.Features.Payments.CreatePayment;
using PaymentService.Application.Features.Payments.FailPayment;
using PaymentService.Application.Features.Payments.GetPaymentById;
using PaymentService.Application.Features.Payments.GetPayments;
using PaymentService.Application.Features.Payments.ProcessPayment;
using PaymentService.Application.Features.Payments.ProcessWebhook;
using PaymentService.Application.Features.Payments.RefundPayment;
using PaymentService.Application.Features.Payments.SearchPayments;

namespace PaymentService.Api.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/payments")
            .WithTags("Payments")
            .RequireAuthorization()
            .RequireRateLimiting("PaymentPolicy");

        group.MapPost("/", async (
            CreatePaymentCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return Results.Created($"/api/v1/payments/{result.PaymentId}", result);
        })
        .WithName("CreatePayment")
        .Produces<CreatePaymentResponse>(StatusCodes.Status201Created)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapGet("/{paymentId:guid}", async (
            Guid paymentId,
            ISender sender,
            CancellationToken ct) =>
        {
            var query = new GetPaymentByIdQuery(paymentId, ct);
            var result = await sender.Send(query, ct);

            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(Result<object>.Failure("Payment not found.", new List<ResultError> { new("NOT_FOUND", "paymentId", $"Payment {paymentId} not found.") }));
        })
        .WithName("GetPaymentById")
        .Produces<PaymentDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/", async (
            [AsParameters] GetPaymentsQuery query,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetPayments")
        .Produces<PagedResult<PaymentDto>>(StatusCodes.Status200OK);

        group.MapPost("/{paymentId:guid}/process", async (
            Guid paymentId,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new ProcessPaymentCommand(paymentId, null, ct);
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("ProcessPayment")
        .Produces<ProcessPaymentResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{paymentId:guid}/confirm", async (
            Guid paymentId,
            ConfirmPaymentCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var updatedCommand = command with { PaymentId = paymentId };
            var result = await sender.Send(updatedCommand, ct);
            return Results.Ok(result);
        })
        .WithName("ConfirmPayment")
        .Produces<ConfirmPaymentResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{paymentId:guid}/fail", async (
            Guid paymentId,
            FailPaymentCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var updatedCommand = command with { PaymentId = paymentId };
            var result = await sender.Send(updatedCommand, ct);
            return Results.Ok(result);
        })
        .WithName("FailPayment")
        .Produces<FailPaymentResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{paymentId:guid}/cancel", async (
            Guid paymentId,
            CancelPaymentCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var updatedCommand = command with { PaymentId = paymentId };
            var result = await sender.Send(updatedCommand, ct);
            return Results.Ok(result);
        })
        .WithName("CancelPayment")
        .Produces<CancelPaymentResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{paymentId:guid}/refund", async (
            Guid paymentId,
            RefundPaymentCommand command,
            ISender sender,
            CancellationToken ct) =>
        {
            var updatedCommand = command with { PaymentId = paymentId };
            var result = await sender.Send(updatedCommand, ct);
            return Results.Ok(result);
        })
        .WithName("RefundPayment")
        .Produces<RefundPaymentResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        var searchGroup = endpoints.MapGroup("/api/v1/payments/search")
            .WithTags("Payments");

        searchGroup.MapGet("/", async (
            [AsParameters] SearchPaymentsQuery query,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("SearchPayments")
        .Produces<PagedResult<PaymentDto>>(StatusCodes.Status200OK);

        var webhookGroup = endpoints.MapGroup("/api/v1/webhooks")
            .WithTags("Webhooks");

        webhookGroup.MapPost("/{providerName}", async (
            string providerName,
            ProcessWebhookCommand command,
            HttpRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var signature = request.Headers["X-Bkash-Signature"].FirstOrDefault()
                         ?? request.Headers["X-Nagad-Signature"].FirstOrDefault()
                         ?? request.Headers["Stripe-Signature"].FirstOrDefault()
                         ?? command.Signature;

            var timestamp = request.Headers["X-Timestamp"].FirstOrDefault();

            var updatedCommand = command with
            {
                ProviderName = providerName,
                Signature = signature,
                Timestamp = string.IsNullOrWhiteSpace(timestamp) ? command.Timestamp : DateTimeOffset.Parse(timestamp)
            };

            var result = await sender.Send(updatedCommand, ct);
            return Results.Ok(result);
        })
        .WithName("ProcessWebhook")
        .Produces<ProcessWebhookResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
