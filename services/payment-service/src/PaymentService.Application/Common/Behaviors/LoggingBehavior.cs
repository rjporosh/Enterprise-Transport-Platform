using Microsoft.Extensions.Logging;
using MediatR;

namespace PaymentService.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var start = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Handling {RequestName} at {Time}",
            requestName,
            start);

        try
        {
            var response = await next();
            var duration = DateTimeOffset.UtcNow - start;

            _logger.LogInformation(
                "Handled {RequestName} in {Duration}ms",
                requestName,
                duration.TotalMilliseconds);

            if (duration.TotalSeconds > 3)
            {
                _logger.LogWarning(
                    "Slow request: {RequestName} took {Duration}ms",
                    requestName,
                    duration.TotalMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - start;
            _logger.LogError(
                ex,
                "Error handling {RequestName} after {Duration}ms. Error: {ErrorMessage}",
                requestName,
                duration.TotalMilliseconds,
                ex.Message);

            throw;
        }
    }
}
