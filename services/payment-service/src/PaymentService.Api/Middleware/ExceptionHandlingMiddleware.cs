using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Models;

namespace PaymentService.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

        var (statusCode, title, errors) = exception switch
        {
            FluentValidation.ValidationException validationEx => (
                400,
                "Validation failed.",
                validationEx.Errors.Select(e => new ResultError(e.ErrorCode, e.PropertyName, e.ErrorMessage)).Cast<object>().ToList()),

            PaymentService.Domain.Exceptions.PaymentNotFoundException => (
                404,
                "Payment not found.",
                new List<object> { new ResultError("NOT_FOUND", "paymentId", exception.Message) }),

            PaymentService.Domain.Exceptions.DuplicatePaymentException => (
                409,
                "Duplicate payment.",
                new List<object> { new ResultError("DUPLICATE", "idempotencyKey", exception.Message) }),

            PaymentService.Domain.Exceptions.InvalidPaymentStateTransitionException => (
                400,
                "Invalid payment state transition.",
                new List<object> { new ResultError("INVALID_TRANSITION", "status", exception.Message) }),

            PaymentService.Domain.Exceptions.InsufficientRefundAmountException => (
                400,
                "Insufficient refund amount.",
                new List<object> { new ResultError("INSUFFICIENT_AMOUNT", "amount", exception.Message) }),

            PaymentService.Domain.Exceptions.PaymentProviderException => (
                502,
                "Payment provider error.",
                new List<object> { new ResultError("PROVIDER_ERROR", "provider", exception.Message) }),

            PaymentService.Domain.Exceptions.DomainException => (
                400,
                "Business rule violation.",
                new List<object> { new ResultError("BUSINESS_RULE", null, exception.Message) }),

            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (
                409,
                "Concurrency conflict.",
                new List<object> { new ResultError("CONCURRENCY", null, "The payment was modified by another request. Please retry.") }),

            _ => (
                500,
                "An unexpected error occurred.",
                new List<object> { new ResultError("INTERNAL_ERROR", null, "An unexpected error occurred. Please contact support.") })
        };

        context.Response.StatusCode = statusCode;

        var problem = Result<object>.Failure(title, errors.Cast<ResultError>().ToList());

        var response = new
        {
            success = false,
            message = title,
            errors = errors,
            traceId = correlationId
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
