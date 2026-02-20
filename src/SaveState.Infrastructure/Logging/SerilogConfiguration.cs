using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Enrichers;

namespace SaveState.Infrastructure.Logging;

/// <summary>
/// Configuration for Serilog structured logging.
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Creates the Serilog logger configuration.
    /// </summary>
    public static LoggerConfiguration CreateConfiguration(IConfiguration configuration)
    {
        var logLevel = configuration.GetValue("Logging:LogLevel:Default", LogEventLevel.Information);
        
        return new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "SaveStateReborn")
            .Enrich.WithProperty("Version", "2.5.1")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code)
            .WriteTo.File(
                path: "logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/log-.json",
                formatter: new CompactJsonFormatter(),
                rollingInterval: RollingInterval.Day);
    }

    /// <summary>
    /// Adds Seq logging if configured.
    /// </summary>
    public static LoggerConfiguration AddSeq(this LoggerConfiguration config, string? seqUrl)
    {
        if (!string.IsNullOrEmpty(seqUrl))
        {
            config.WriteTo.Seq(seqUrl);
        }
        return config;
    }
}
