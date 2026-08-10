using MediatR;
using NotificationService.Api.Common;
using NotificationService.Application.Features.Release;

namespace NotificationService.Api.Endpoints;

public static class ReleaseEndpoints
{
    public static void MapReleaseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/release", GetReleaseAsync)
            .WithName("GetReleaseInfo")
            .WithSummary("SQA/tester endpoint: returns current service release/change information.")
            .WithTags("Release")
            .Produces<ApiResponse<ReleaseInfoDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetReleaseAsync(IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetReleaseInfoQuery(), cancellationToken);
        return result.ToApiResult(httpContext);
    }
}
