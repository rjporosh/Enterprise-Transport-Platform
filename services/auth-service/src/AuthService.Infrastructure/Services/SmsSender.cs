using AuthService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Services;

public sealed class SmsSender : ISmsSender
{
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(ILogger<SmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SMS to {Phone}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}
