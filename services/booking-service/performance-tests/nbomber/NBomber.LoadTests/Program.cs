// NBomber .NET-native load & stress test suite — the same two scenarios as
// ../../k6/search-trips-load-test.js and create-booking-stress-test.js, for
// teams that want performance tests written in C# alongside the service
// itself (shared CI pipeline, shared debugging tools, no separate JS/Java
// runtime needed).
//
// Run:
//   dotnet run -c Release -- --scenario search
//   dotnet run -c Release -- --scenario stress --trip-id <seeded-trip-id> --token <dev-jwt>
//
// See README.md in this folder for the full step-by-step.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Http.CSharp;

var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:8080";
var scenarioName = GetArg(args, "--scenario") ?? "search";

using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

if (scenarioName == "search")
{
    RunSearchLoadTest(httpClient);
}
else if (scenarioName == "stress")
{
    var tripId = GetArg(args, "--trip-id")
        ?? throw new ArgumentException("--trip-id is required for the stress scenario (seed one via scripts/seed-demo-data.sql).");
    var token = GetArg(args, "--token")
        ?? throw new ArgumentException("--token is required for the stress scenario (a dev JWT — see postman/README.md for how to mint one).");
    var seatNumber = GetArg(args, "--seat") ?? "A1";

    RunCreateBookingStressTest(httpClient, tripId, token, seatNumber);
}
else
{
    Console.WriteLine($"Unknown scenario '{scenarioName}'. Use --scenario search or --scenario stress.");
}

return;

static string? GetArg(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

// --- Scenario 1: steady load against the search endpoint -------------------
static void RunSearchLoadTest(HttpClient httpClient)
{
    var routes = new[]
    {
        ("Dhaka", "Chattogram"),
        ("Dhaka", "Sylhet"),
        ("Chattogram", "Cox's Bazar")
    };
    var random = new Random();

    var scenario = Scenario.Create("search_trips_load", async context =>
    {
        var (origin, destination) = routes[random.Next(routes.Length)];
        var url = $"/api/v1/trips/search?origin={origin}&destination={destination}&date=2026-08-15&page=1&pageSize=10";

        var request = Http.CreateRequest("GET", url);
        var response = await Http.Send(httpClient, request);

        return response;
    })
    .WithLoadSimulations(
        Simulation.RampingInject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
        Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
        Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
    );

    NBomberRunner
        .RegisterScenarios(scenario)
        .WithReportFolder("reports/search-trips-load-test")
        .Run();
}

// --- Scenario 2: 50 concurrent bookers racing for the same seat -----------
// Correctness assertion (checked manually from the report, NBomber doesn't
// have a built-in "at most N successes" assertion): count how many requests
// in the report got a 201 for this seat. It must be exactly 1. Every other
// request must be a 409. Anything else (500, timeout) is a real bug.
static void RunCreateBookingStressTest(HttpClient httpClient, string tripId, string token, string seatNumber)
{
    var scenario = Scenario.Create("create_booking_seat_contention", async context =>
    {
        var payload = new
        {
            tripId,
            customerId = $"00000000-0000-0000-0000-{context.ScenarioInfo.ThreadId:D12}",
            passengers = new[]
            {
                new { seatNumber, fullName = $"NBomber VU {context.ScenarioInfo.ThreadId}", age = 30, gender = "Male" }
            }
        };

        var request = Http.CreateRequest("POST", "/api/v1/bookings")
            .WithHeader("Authorization", $"Bearer {token}")
            .WithJsonBody(payload);

        var response = await Http.Send(httpClient, request);

        // 201 (won the seat) and 409 (lost the race, correctly rejected) are
        // BOTH considered "ok" from NBomber's perspective — we're not
        // measuring success rate here, we're measuring correctness, which
        // gets checked by reading the report afterwards.
        return response;
    })
    .WithLoadSimulations(
        Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(1))
    );

    NBomberRunner
        .RegisterScenarios(scenario)
        .WithReportFolder("reports/create-booking-stress-test")
        .Run();
}
