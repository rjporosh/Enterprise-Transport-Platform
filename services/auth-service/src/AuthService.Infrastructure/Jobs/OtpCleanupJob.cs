using AuthService.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace AuthService.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class OtpCleanupJob : IJob
{
    private readonly IOtpService _otpService;
    private readonly ILogger<OtpCleanupJob> _logger;

    public OtpCleanupJob(IOtpService otpService, ILogger<OtpCleanupJob> logger)
    {
        _otpService = otpService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await _otpService.CleanupExpiredOtpsAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTP cleanup job failed.");
            throw;
        }
    }
}
