using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SaveState.Infrastructure.HealthChecks;

/// <summary>
/// Extension methods for registering health checks.
/// </summary>
public static class HealthCheckRegistration
{
    /// <summary>
    /// Adds SaveState health checks to the service collection.
    /// </summary>
    public static IServiceCollection AddSaveStateHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "database",
                tags: new[] { "database", "core" })
            .AddCheck<DiskSpaceHealthCheck>(
                "disk-space",
                tags: new[] { "system", "core" })
            .AddCheck<MemoryHealthCheck>(
                "memory",
                tags: new[] { "system", "core" })
            .AddCheck<ExternalApiHealthCheck>(
                "external-apis",
                tags: new[] { "external", "optional" });

        // Register our custom health check service wrapper
        services.AddSingleton<ApplicationHealthCheckService>();

        return services;
    }

    /// <summary>
    /// Adds health checks with custom configuration.
    /// </summary>
    public static IServiceCollection AddSaveStateHealthChecks(
        this IServiceCollection services,
        Action<HealthCheckOptions>? configureOptions = null)
    {
        var builder = services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "database", "core" })
            .AddCheck<DiskSpaceHealthCheck>("disk-space", tags: new[] { "system", "core" })
            .AddCheck<MemoryHealthCheck>("memory", tags: new[] { "system", "core" });

        configureOptions?.Invoke(new HealthCheckOptions());

        // Register our custom health check service wrapper
        services.AddSingleton<ApplicationHealthCheckService>();

        return services;
    }
}

/// <summary>
/// Configuration options for health checks.
/// </summary>
public class HealthCheckOptions
{
    /// <summary>
    /// Minimum free disk space in bytes (default: 1GB).
    /// </summary>
    public long MinimumFreeDiskSpaceBytes { get; set; } = 1_000_000_000;

    /// <summary>
    /// Maximum memory usage percentage before degraded (default: 90%).
    /// </summary>
    public int MaxMemoryUsagePercent { get; set; } = 90;

    /// <summary>
    /// Timeout for external API health checks.
    /// </summary>
    public TimeSpan ExternalApiTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
