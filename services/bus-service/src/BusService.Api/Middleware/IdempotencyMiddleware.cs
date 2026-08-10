using System.Text.Json;
using BusService.Application.Common.Interfaces;
using BusService.Infrastructure.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BusService.Api.Middleware;

public sealed class IdempotencyMiddleware
{
    private const string HeaderName = "Idempotency-Key";
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private readonly ICacheService _cache;
    private readonly RedisOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger, ICacheService cache, IOptions<RedisOptions> options)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var idempotencyKey) ||
            string.IsNullOrWhiteSpace(idempotencyKey) ||
            !IsWriteMethod(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var cacheKey = $"idempotency:{idempotencyKey}:{context.Request.Path}";
        var cached = await _cache.GetAsync<IdempotencyResponse>(cacheKey, context.RequestAborted);
        if (cached is not null)
        {
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            await context.Response.WriteAsync(cached.Body, context.RequestAborted);
            _logger.LogInformation("Returned idempotent response for key {Key} on {Path}", idempotencyKey, context.Request.Path);
            return;
        }

        var originalBody = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(memoryStream).ReadToEndAsync(context.RequestAborted);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var response = new IdempotencyResponse
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType ?? "application/json",
                Body = body
            };

            await _cache.SetAsync(cacheKey, response, TimeSpan.FromHours(24), context.RequestAborted);

            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalBody, context.RequestAborted);
        }
        catch (Exception)
        {
            context.Response.Body = originalBody;
            throw;
        }
    }

    private static bool IsWriteMethod(string method) =>
        method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase) ||
        method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase) ||
        method.Equals(HttpMethods.Patch, StringComparison.OrdinalIgnoreCase) ||
        method.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase);
}

public sealed class IdempotencyResponse
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = "application/json";
    public string Body { get; set; } = string.Empty;
}
