using System;
using System.Net.Http;
using System.Threading.Tasks;
using NBomber.Contract;
using NBomber.Contract.Stats;
using NBomber.CSharp;

namespace RouteService.LoadTests;

public class RouteServiceLoadTests
{
    public static void Main(string[] args)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5003") };

        var getRoutes = Step.Create("get_routes", clientFactory: _ => httpClient, execute: async (ctx, client) =>
        {
            var response = await client.GetAsync("/api/v1/routes?page=1&pageSize=20");
            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        });

        var scenario = ScenarioBuilder.CreateScenario("route_service_load", getRoutes)
            .WithLoadSimulations(LoadSimulation.NewRampConstant(50, TimeSpan.FromSeconds(30)));

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }
}
