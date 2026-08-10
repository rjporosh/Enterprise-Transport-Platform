using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RouteService.Application.Common.Interfaces;

namespace RouteService.Infrastructure.Communication;

public sealed class HttpCommunicationService : ICommunicationService
{
    private readonly HttpClient _httpClient;
    private readonly CommunicationOptions _options;
    private readonly ILogger<HttpCommunicationService> _logger;

    public HttpCommunicationService(HttpClient httpClient, IOptions<CommunicationOptions> options, ILogger<HttpCommunicationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
    }
}
