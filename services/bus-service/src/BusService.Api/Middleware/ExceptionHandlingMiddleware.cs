using System.Net;
using BusService.Application.Common.Models;
using BusService.Domain.Exceptions;
using FluentValidation;

namespace BusService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
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
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException => (
                HttpStatusCode.BadRequest,
                "One or more validation errors occurred.",
                validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .SelectMany(g => g.Select(e => new ResultError(
                        Code: "VALIDATION_ERROR",
                        Field: e.PropertyName,
                        Message: e.ErrorMessage)))
                    .ToList()),

            BusNotFoundException => (
                HttpStatusCode.NotFound,
                "Bus not found.",
                new List<ResultError> { new("BUS_NOT_FOUND", exception.Message, exception.Message) }),

            DepotNotFoundException => (
                HttpStatusCode.NotFound,
                "Depot not found.",
                new List<ResultError> { new("DEPOT_NOT_FOUND", exception.Message, exception.Message) }),

            DuplicatePlateNumberException => (
                HttpStatusCode.Conflict,
                "Duplicate plate number.",
                new List<ResultError> { new("DUPLICATE_PLATE_NUMBER", "plateNumber", exception.Message) }),

            InvalidBusStatusTransitionException => (
                HttpStatusCode.BadRequest,
                "Invalid bus status transition.",
                new List<ResultError> { new("INVALID_STATUS_TRANSITION", "status", exception.Message) }),

            ConcurrencyException => (
                HttpStatusCode.Conflict,
                "Concurrency conflict.",
                new List<ResultError> { new("CONCURRENCY_CONFLICT", string.Empty, "The record was modified by another user. Please refresh and try again.") }),

            DomainException => (
                HttpStatusCode.BadRequest,
                "A domain error occurred.",
                new List<ResultError> { new("DOMAIN_ERROR", string.Empty, exception.Message) }),

            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                new List<ResultError> { new("INTERNAL_ERROR", string.Empty, "An unexpected error occurred. Please try again later.") })
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception processing {Path}", context.Request.Path);
        else
            _logger.LogInformation("Handled {ExceptionType} as {StatusCode}: {Message}", exception.GetType().Name, (int)statusCode, exception.Message);

        var result = new Result
        {
            Success = false,
            Message = title,
            TraceId = context.TraceIdentifier
        };
        result.Errors.AddRange(errors);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(result);
    }
}
