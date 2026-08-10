using Microsoft.AspNetCore.Mvc;

namespace RouteService.Api.Endpoints;

public static class ReleaseEndpoints
{
    public static void MapReleaseEndpoints(this IEndpointRouteBuilder app)
    {
        var release = app.MapGroup("/api/v1/release").WithTags("Release").AllowAnonymous();

        release.MapGet("/info", GetReleaseInfo)
            .WithName("GetReleaseInfo")
            .WithSummary("Get current release information for SQA/testers.")
            .Produces<object>(StatusCodes.Status200OK);
    }

    private static IResult GetReleaseInfo(HttpContext context)
    {
        var response = new
        {
            Service = "Route Service",
            Version = "1.0.0",
            Environment = context.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()?.EnvironmentName ?? "Unknown",
            Timestamp = DateTimeOffset.UtcNow,
            BuildNumber = Environment.GetEnvironmentVariable("BUILD_BUILDID") ?? "local",
            Commit = Environment.GetEnvironmentVariable("BUILD_SOURCEVERSION") ?? "unknown",
            Features = new[]
            {
                "Route CRUD",
                "Stop CRUD",
                "Schedule CRUD",
                "Soft Delete",
                "Optimistic Concurrency",
                "Pagination / Filtering / Search",
                "Audit Logging",
                "Localization (en, bn)",
                "REST + gRPC",
                "RabbitMQ Event Publishing",
                "OpenTelemetry",
                "Serilog",
                "Health Checks",
                "Rate Limiting"
            }
        };

        return Results.Ok(response);
    }
}
