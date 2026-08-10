using Microsoft.Extensions.Logging;
using System.Text;

namespace PaymentService.Api.Diagnostics;

public static class RuntimeErrorLogWriter
{
    private static readonly string LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs", "runtime-error-logs");
    private static readonly object _lock = new();

    public static void Write(Exception ex, string? endpoint = null, string? httpMethod = null)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var fileName = $"runtime-error-{DateTime.UtcNow:dd-MM-yyyy}.txt";
            var filePath = Path.Combine(LogDirectory, fileName);

            var now = DateTime.UtcNow;
            var logEntry = new StringBuilder();

            logEntry.AppendLine("============================================================");
            logEntry.AppendLine("RUNTIME ERROR");
            logEntry.AppendLine("============================================================");
            logEntry.AppendLine($"Timestamp: {now:O}");
            logEntry.AppendLine($"Service: PaymentService");
            logEntry.AppendLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"}");
            logEntry.AppendLine($"Endpoint: {endpoint ?? "N/A"}");
            logEntry.AppendLine($"HttpMethod: {httpMethod ?? "N/A"}");
            logEntry.AppendLine($"ClassName: {ex.Source ?? "Unknown"}");
            logEntry.AppendLine($"ExceptionType: {ex.GetType().FullName}");
            logEntry.AppendLine($"ExactExceptionMessage: {ex.Message}");
            logEntry.AppendLine($"StackTrace:{Environment.NewLine}{ex.StackTrace}");

            if (ex.InnerException != null)
            {
                logEntry.AppendLine($"InnerException:{Environment.NewLine}{ex.InnerException.Message}{Environment.NewLine}{ex.InnerException.StackTrace}");
            }

            logEntry.AppendLine($"RootCause: {GetRootCause(ex)}");
            logEntry.AppendLine($"PossibleSolution: {GetPossibleSolution(ex)}");
            logEntry.AppendLine();

            lock (_lock)
            {
                File.AppendAllText(filePath, logEntry.ToString());
            }
        }
        catch
        {
            // Log file writing failed - original exception should still propagate
        }
    }

    private static string GetRootCause(Exception ex)
    {
        return ex switch
        {
            Npgsql.NpgsqlException => "PostgreSQL database error.",
            TimeoutException => "Operation timed out.",
            System.Net.Http.HttpRequestException => "External HTTP request failed.",
            RabbitMQ.Client.Exceptions.BrokerUnreachableException => "RabbitMQ broker is unreachable.",
            StackExchange.Redis.RedisException => "Redis connection error.",
            _ => "Unknown runtime error. Check inner exception details."
        };
    }

    private static string GetPossibleSolution(Exception ex)
    {
        return ex switch
        {
            Npgsql.NpgsqlException => "Verify database connection string, schema, and migrations.",
            TimeoutException => "Check network connectivity and increase timeout if appropriate.",
            System.Net.Http.HttpRequestException => "Verify external service availability and network configuration.",
            RabbitMQ.Client.Exceptions.BrokerUnreachableException => "Verify RabbitMQ is running and credentials are correct.",
            StackExchange.Redis.RedisException => "Verify Redis is running and connection string is correct.",
            _ => "Investigate inner exception and verify service configuration."
        };
    }
}
