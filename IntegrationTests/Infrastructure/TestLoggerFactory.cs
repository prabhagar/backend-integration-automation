using IntegrationTests.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace IntegrationTests.Infrastructure;

public static class TestLoggerFactory
{
    public static ILoggerFactory Create(LoggingSettings settings)
    {
        Directory.CreateDirectory(settings.LogsDirectory);

        var minimumLevel = settings.MinimumLevel.ToLowerInvariant() switch
        {
            "debug" => Serilog.Events.LogEventLevel.Debug,
            "warning" => Serilog.Events.LogEventLevel.Warning,
            "error" => Serilog.Events.LogEventLevel.Error,
            _ => Serilog.Events.LogEventLevel.Information
        };

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .Enrich.WithProperty("Suite", "BackendIntegrationAutomation")
            .WriteTo.Console()
            .WriteTo.File(
                path: Path.Combine(settings.LogsDirectory, "day1-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        return LoggerFactory.Create(builder => builder.AddSerilog(logger, dispose: true));
    }
}