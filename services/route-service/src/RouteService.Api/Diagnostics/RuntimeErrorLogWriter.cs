using System.Text;
using Microsoft.Extensions.FileProviders;

namespace RouteService.Api.Diagnostics;

public static class RuntimeErrorLogWriter
{
    public static string Write(Exception ex, string contentRoot)
    {
        var logsDir = Path.Combine(contentRoot, "logs");
        Directory.CreateDirectory(logsDir);
        var fileName = $"runtime-error-{DateTime.UtcNow:dd-MM-yyyy-HH-mm-ss}.txt";
        var path = Path.Combine(logsDir, fileName);

        var text = new StringBuilder()
            .AppendLine($"Timestamp: {DateTime.UtcNow:O}")
            .AppendLine($"Exception: {ex.GetType().FullName}")
            .AppendLine($"Message: {ex.Message}")
            .AppendLine($"StackTrace:{Environment.NewLine}{ex.StackTrace}")
            .ToString();

        File.WriteAllText(path, text);
        return path;
    }
}
