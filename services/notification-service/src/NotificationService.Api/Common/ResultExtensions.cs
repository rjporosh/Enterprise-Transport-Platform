using Microsoft.AspNetCore.Http.HttpResults;
using NotificationService.Application.Common.Models;

namespace NotificationService.Api.Common;

public static class ResultExtensions
{
    public static IResult ToApiResult<T>(this Result<T> result, HttpContext httpContext, string? successMessage = null)
    {
        if (result.IsSuccess)
            return Results.Ok(ApiResponse<T>.Ok(result.Value!, httpContext.TraceIdentifier, successMessage ?? "Request completed successfully."));

        return ToErrorResult<T>(result.Errors, httpContext);
    }

    public static IResult ToCreatedApiResult<T>(this Result<T> result, HttpContext httpContext, string location, string? successMessage = null)
    {
        if (result.IsSuccess)
            return Results.Created(location, ApiResponse<T>.Ok(result.Value!, httpContext.TraceIdentifier, successMessage ?? "Resource created successfully."));

        return ToErrorResult<T>(result.Errors, httpContext);
    }

    public static IResult ToApiResult(this Result result, HttpContext httpContext, string? successMessage = null)
    {
        if (result.IsSuccess)
            return Results.Ok(ApiResponse<object>.Ok(new { }, httpContext.TraceIdentifier, successMessage ?? "Request completed successfully."));

        return ToErrorResult<object>(result.Errors, httpContext);
    }

    private static IResult ToErrorResult<T>(IReadOnlyList<Error> errors, HttpContext httpContext)
    {
        var items = errors.Select(e => new ApiErrorItem(e.Code, e.Message, e.Field)).ToList();
        var response = ApiResponse<T>.Fail(items, httpContext.TraceIdentifier);

        // First error's code decides the HTTP status; a batch of validation
        // errors is always all-VALIDATION_ERROR so this is unambiguous in
        // practice, and mixed-code batches don't occur in this codebase's
        // handlers today.
        var statusCode = errors.First().Code switch
        {
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "CONFLICT" => StatusCodes.Status409Conflict,
            "INVALID_STATE" => StatusCodes.Status409Conflict,
            "VALIDATION_ERROR" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Json(response, statusCode: statusCode);
    }
}
