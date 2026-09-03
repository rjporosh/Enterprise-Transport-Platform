using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Common.Models;
using PaymentService.Application.Features.Payments;
using PaymentService.Application.Features.Payments.CancelPayment;
using PaymentService.Application.Features.Payments.ConfirmPayment;
using PaymentService.Application.Features.Payments.CreatePayment;
using PaymentService.Application.Features.Payments.FailPayment;
using PaymentService.Application.Features.Payments.GenerateQr;
using PaymentService.Application.Features.Payments.GetPaymentById;
using PaymentService.Application.Features.Payments.GetPayments;
using PaymentService.Application.Features.Payments.SettleQr;
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
            PaymentService.Application.Common.Interfaces.ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            // Tenant + customer identity always come from the validated token,
            // never the request body (P0-10 / P0-11). A privileged caller
            // (Admin/Operator raising a payment on someone's behalf) may keep
            // the body values.
            var privileged = currentUser.IsInRole("Admin") || currentUser.IsInRole("Operator");
            var tenantId = Guid.TryParse(currentUser.TenantId, out var t) ? t : command.TenantId;
            var customerId = currentUser.CustomerId ?? command.CustomerId;

            var effective = command with
            {
                TenantId = privileged ? command.TenantId : tenantId,
                CustomerId = privileged ? command.CustomerId : customerId
            };

            var result = await sender.Send(effective, ct);
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

        group.MapPost("/{paymentId:guid}/qr", async (
            Guid paymentId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GenerateQrCommand(paymentId, ct), ct);
            return Results.Ok(result);
        })
        .WithName("GenerateQr")
        .WithSummary("Generate a genuine EMVCo / Bangla-QR for a QR payment.")
        .WithDescription("Moves the payment to Processing and returns a spec-correct EMVCo merchant-presented " +
                         "QR payload + PNG data URI. The customer scans it with any bank / MFS app. Settlement " +
                         "arrives via POST /api/v1/webhooks/qr (signed) or the audited admin settle-qr endpoint.")
        .Produces<GenerateQrResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{paymentId:guid}/settle-qr", async (
            Guid paymentId,
            SettleQrRequest body,
            PaymentService.Application.Common.Interfaces.ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new SettleQrCommand(
                paymentId,
                string.IsNullOrWhiteSpace(body?.SettlementReference) ? $"MANUAL-{paymentId:N}" : body!.SettlementReference,
                currentUser.UserId?.ToString() ?? "operator",
                ct);
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"))
        .WithName("SettleQr")
        .WithSummary("Record that a QR payment has settled (audited demo stand-in for a live acquirer callback).")
        .Produces<SettleQrResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
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

        // NOTE: this group previously had no .RequireAuthorization()/.RequireRateLimiting(),
        // unlike every other group in this file — meaning SearchPayments was silently
        // callable without a token. Payment records include CustomerId/TenantId/Amount,
        // so an unauthenticated search endpoint is a data-exposure bug, not an
        // intentional public endpoint (contrast with the webhook group below, which is
        // deliberately unauthenticated because payment providers can't obtain a
        // platform JWT — that one is protected by provider signature verification
        // instead). Fixed to match the rest of the service.
        var searchGroup = endpoints.MapGroup("/api/v1/payments/search")
            .WithTags("Payments")
            .RequireAuthorization()
            .RequireRateLimiting("PaymentPolicy");

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
            // Unknown provider / bad signature / unparseable payload → 400, not a 200 with success:false.
            return result.Success
                ? Results.Ok(result)
                : Results.BadRequest(new { success = false, message = result.Error ?? "Webhook rejected.", status = result.Status });
        })
        .WithName("ProcessWebhook")
        .Produces<ProcessWebhookResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // QR settlement callback from an acquirer. HMAC-SHA256 over the raw body
        // with Payments:Qr:WebhookSigningKey. Rejects everything when the key is
        // unset (settle via the audited admin endpoint instead).
        webhookGroup.MapPost("/qr", async (
            HttpRequest request,
            PaymentService.Application.Common.Interfaces.IPaymentProviderFactory providers,
            ISender sender,
            CancellationToken ct) =>
        {
            using var reader = new StreamReader(request.Body);
            var rawBody = await reader.ReadToEndAsync(ct);
            var signature = request.Headers["X-Qr-Signature"].FirstOrDefault();

            if (!providers.GetProvider("Qr").VerifyWebhookSignature(rawBody, signature, null))
                return Results.BadRequest(new { success = false, message = "Invalid or unconfigured QR webhook signature." });

            using var doc = System.Text.Json.JsonDocument.Parse(rawBody);
            if (!doc.RootElement.TryGetProperty("paymentId", out var pid) || !Guid.TryParse(pid.GetString(), out var paymentId))
                return Results.BadRequest(new { success = false, message = "paymentId missing." });

            var reference = doc.RootElement.TryGetProperty("settlementReference", out var r) ? r.GetString() : null;
            var result = await sender.Send(new SettleQrCommand(paymentId, reference ?? $"QR-{paymentId:N}", "acquirer-webhook", ct), ct);
            return Results.Ok(result);
        })
        .WithName("QrSettlementWebhook")
        .Produces<SettleQrResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}

public sealed record SettleQrRequest(string? SettlementReference);
