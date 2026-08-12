using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace PaymentLoadTests;

public class Program
{
    public static void Main(string[] args)
    {
        var baseUrl = args.FirstOrDefault() ?? "http://localhost:5003";
        var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", args.ElementAtOrDefault(1) ?? string.Empty);

        var createPayment = Http.CreateRequest("Create Payment", client => client
            .WithMethod("POST")
            .WithPath("/api/v1/payments")
            .WithHeader("Content-Type", "application/json")
            .WithBody(new
            {
                TenantId = "11111111-1111-1111-1111-111111111111",
                CustomerId = "44444444-4444-4444-4444-444444444444",
                OrderReference = $"NBOMBER-{Guid.NewGuid():N}",
                PaymentMethod = "Card",
                Amount = 100.00,
                Currency = "USD",
                IdempotencyKey = $"nbomber-{Guid.NewGuid():N}",
                TtlMinutes = 30
            }))
            .WithCheck(response => response.StatusCode == System.Net.HttpStatusCode.Created);

        var scenario = ScenarioBuilder
            .CreateScenario("payment_load_test", createPayment)
            .WithLoadSimulations(LoadSimulation.NewInjectPerSec(10, TimeSpan.FromSeconds(30)));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFormats(ReportFormat.Txt, ReportFormat.Html)
            .WithReportFolder("test-results/nbomber")
            .Run();

        Console.WriteLine($"RPS: {result.ScenarioStats.GetScenarioStats("payment_load_test").Ok.RPS}");
    }
}
