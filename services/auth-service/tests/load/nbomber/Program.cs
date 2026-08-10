using System.Net.Http.Json;
using System.Text.Json;
using NBomber.CSharp;
using NBomber.Http.CSharp;

// .NET-native equivalent of tests/load/k6/login-load-test.js — same shape
// (register a user pool in setup, hammer /login), useful for teams that
// want load scenarios written in C# alongside the codebase instead of a
// separate k6/JMeter toolchain. See ../README.md, "Which tool should I use".
//
// Run: dotnet run -c Release -- --base-url http://localhost:8081

var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:8081";
var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

const string password = "correct-horse-battery-staple";
const int userPoolSize = 50;
var registeredEmails = new List<string>();

Console.WriteLine($"Registering a pool of {userPoolSize} users against {baseUrl} ...");
for (var i = 0; i < userPoolSize; i++)
{
    var email = $"nbomber-{i}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}@example.com";
    var response = await httpClient.PostAsJsonAsync("/api/v1/auth/register", new
    {
        email,
        password,
        firstName = "Nbomber",
        lastName = $"User{i}",
        phoneNumber = (string?)null
    });

    if (response.IsSuccessStatusCode)
        registeredEmails.Add(email);
}
Console.WriteLine($"Registered {registeredEmails.Count} users. Starting load scenario...");

var loginScenario = Scenario.Create("login_load", async context =>
{
    var email = registeredEmails[Random.Shared.Next(registeredEmails.Count)];

    var request = Http.CreateRequest("POST", "/api/v1/auth/login")
        .WithHeader("Content-Type", "application/json")
        .WithBody(new StringContent(JsonSerializer.Serialize(new { email, password })));

    var response = await Http.Send(httpClient, request);

    return response.StatusCode == "200" ? Response.Ok(statusCode: response.StatusCode) : Response.Fail(statusCode: response.StatusCode);
})
.WithLoadSimulations(
    // Same ramp shape as the k6 login load test: 30s ramp-up to 25 RPS-ish
    // concurrent, 2min hold, 30s ramp-down.
    Simulation.RampingInject(rate: 25, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.Inject(rate: 25, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
    Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
);

NBomberRunner
    .RegisterScenarios(loginScenario)
    .WithReportFolder("reports")
    .WithReportFormats(NBomber.Contracts.Stats.ReportFormat.Html, NBomber.Contracts.Stats.ReportFormat.Csv)
    .Run();

var otpScenario = Scenario.Create("auth_flow_with_otp", async context =>
{
    var email = registeredEmails[Random.Shared.Next(registeredEmails.Count)];

    var loginRequest = Http.CreateRequest("POST", "/api/v1/auth/login")
        .WithHeader("Content-Type", "application/json")
        .WithBody(new StringContent(JsonSerializer.Serialize(new { email, password })));

    var loginResponse = await Http.Send(httpClient, loginRequest);
    if (loginResponse.StatusCode != "200")
        return Response.Fail(statusCode: loginResponse.StatusCode);

    var loginBody = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
    var userId = loginBody.RootElement.GetProperty("userId").GetString();

    var otpRequest = Http.CreateRequest("POST", "/api/v1/auth/otp/request")
        .WithHeader("Content-Type", "application/json")
        .WithBody(new StringContent(JsonSerializer.Serialize(new { userId, channel = "email", destination = email })));

    var otpResponse = await Http.Send(httpClient, otpRequest);
    return otpResponse.StatusCode == "204" ? Response.Ok(statusCode: otpResponse.StatusCode) : Response.Fail(statusCode: otpResponse.StatusCode);
})
.WithLoadSimulations(
    Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
    Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
);

NBomberRunner
    .RegisterScenarios(loginScenario, otpScenario)
    .WithReportFolder("reports")
    .WithReportFormats(NBomber.Contracts.Stats.ReportFormat.Html, NBomber.Contracts.Stats.ReportFormat.Csv)
    .Run();
