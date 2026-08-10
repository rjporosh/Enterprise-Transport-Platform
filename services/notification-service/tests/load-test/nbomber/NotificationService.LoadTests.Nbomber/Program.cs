using System.Net.Http.Json;
using System.Text.Json;
using NBomber.CSharp;
using NBomber.Http.CSharp;

// .NET-native load and stress tests for Notification Service.
// Run: dotnet run -c Release -- --base-url http://localhost:5301

var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:5301";
var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine($"Target: {baseUrl}");

// ===== Load Scenario =====
var sendLoadScenario = Scenario.Create("send_notification_load", async context =>
{
    var payload = JsonSerializer.Serialize(new
    {
        recipient = $"loadtest-{Guid.NewGuid()}@example.com",
        channel = "Email",
        subject = "Load test",
        body = "This is a NBomber load test notification.",
        priority = "Normal",
        isTransactional = true
    });

    var request = Http.CreateRequest("POST", "/api/v1/notifications")
        .WithHeader("Content-Type", "application/json")
        .WithBody(new StringContent(payload));

    var response = await Http.Send(httpClient, request);

    return response.StatusCode == "201" ? Response.Ok(statusCode: response.StatusCode) : Response.Fail(statusCode: response.StatusCode);
})
.WithLoadSimulations(
    Simulation.RampingInject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
    Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
);

// ===== Stress Scenario =====
// Ramp up until the service degrades, recording the breaking point.
var sendStressScenario = Scenario.Create("send_notification_stress", async context =>
{
    var payload = JsonSerializer.Serialize(new
    {
        recipient = $"stress-{Guid.NewGuid()}@example.com",
        channel = "Email",
        subject = "Stress test",
        body = "This is a NBomber stress test notification.",
        priority = "Normal",
        isTransactional = true
    });

    var request = Http.CreateRequest("POST", "/api/v1/notifications")
        .WithHeader("Content-Type", "application/json")
        .WithBody(new StringContent(payload));

    var response = await Http.Send(httpClient, request);

    return response.StatusCode == "201" ? Response.Ok(statusCode: response.StatusCode) : Response.Fail(statusCode: response.StatusCode);
})
.WithLoadSimulations(
    Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
    Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
    Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
);

// ===== Query Scenario =====
// Paged listing under read load.
var getNotificationsScenario = Scenario.Create("get_notifications_load", async context =>
{
    var request = Http.CreateRequest("GET", $"/api/v1/notifications?page=1&pageSize=20");

    var response = await Http.Send(httpClient, request);

    return response.StatusCode == "200" ? Response.Ok(statusCode: response.StatusCode) : Response.Fail(statusCode: response.StatusCode);
})
.WithLoadSimulations(
    Simulation.Inject(rate: 30, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
);

NBomberRunner
    .RegisterScenarios(sendLoadScenario, sendStressScenario, getNotificationsScenario)
    .WithReportFolder("reports")
    .WithReportFormats(NBomber.Contracts.Stats.ReportFormat.Html, NBomber.Contracts.Stats.ReportFormat.Csv)
    .Run();
