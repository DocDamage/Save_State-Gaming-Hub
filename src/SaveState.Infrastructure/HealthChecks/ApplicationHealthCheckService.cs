using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using System.Diagnostics;
using System.Text.Json;

namespace SaveState.Infrastructure.HealthChecks;

/// <summary>
/// Service for running health checks in desktop applications.
/// </summary>
public class ApplicationHealthCheckService
{
    private readonly Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService _healthCheckService;
    private readonly ILogger<ApplicationHealthCheckService> _logger;
    private readonly ITimeProvider _timeProvider;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ApplicationHealthCheckService(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService,
        ILogger<ApplicationHealthCheckService> logger,
        ITimeProvider timeProvider)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Runs all health checks and returns the report.
    /// </summary>
    public async Task<HealthCheckResponse> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
            stopwatch.Stop();

            return new HealthCheckResponse
            {
                Status = report.Status.ToString().ToLowerInvariant(),
                Timestamp = _timeProvider.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Checks = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new HealthCheckItem
                    {
                        Status = entry.Value.Status.ToString().ToLowerInvariant(),
                        Description = entry.Value.Description,
                        DurationMs = (long)entry.Value.Duration.TotalMilliseconds,
                        Data = entry.Value.Data?.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value)
                    })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            stopwatch.Stop();

            return new HealthCheckResponse
            {
                Status = "unhealthy",
                Timestamp = _timeProvider.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Checks = new Dictionary<string, HealthCheckItem>
                {
                    ["error"] = new HealthCheckItem
                    {
                        Status = "unhealthy",
                        Description = $"Health check execution failed: {ex.Message}"
                    }
                }
            };
        }
    }

    /// <summary>
    /// Runs health checks for specific tags.
    /// </summary>
    public async Task<HealthCheckResponse> CheckHealthByTagsAsync(
        string[] tags, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var report = await _healthCheckService.CheckHealthAsync(
                check => tags.Any(tag => check.Tags.Contains(tag)),
                cancellationToken);
            
            stopwatch.Stop();

            return new HealthCheckResponse
            {
                Status = report.Status.ToString().ToLowerInvariant(),
                Timestamp = _timeProvider.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Checks = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new HealthCheckItem
                    {
                        Status = entry.Value.Status.ToString().ToLowerInvariant(),
                        Description = entry.Value.Description,
                        DurationMs = (long)entry.Value.Duration.TotalMilliseconds,
                        Data = entry.Value.Data?.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value)
                    })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            stopwatch.Stop();

            return new HealthCheckResponse
            {
                Status = "unhealthy",
                Timestamp = _timeProvider.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Checks = new Dictionary<string, HealthCheckItem>()
            };
        }
    }

    /// <summary>
    /// Serializes the health check response to JSON.
    /// </summary>
    public static string SerializeToJson(HealthCheckResponse response)
    {
        return JsonSerializer.Serialize(response, _jsonOptions);
    }
}

/// <summary>
/// Extension methods for health status.
/// </summary>
public static class HealthStatusExtensions
{
    /// <summary>
    /// Determines if the health status is healthy.
    /// </summary>
    public static bool IsHealthy(this HealthCheckResponse response)
    {
        return response.Status.Equals("healthy", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if the health status is degraded or unhealthy.
    /// </summary>
    public static bool HasIssues(this HealthCheckResponse response)
    {
        return response.Status.Equals("degraded", StringComparison.OrdinalIgnoreCase) ||
               response.Status.Equals("unhealthy", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all unhealthy checks.
    /// </summary>
    public static IEnumerable<KeyValuePair<string, HealthCheckItem>> GetUnhealthyChecks(
        this HealthCheckResponse response)
    {
        return response.Checks.Where(c => 
            c.Value.Status.Equals("unhealthy", StringComparison.OrdinalIgnoreCase) ||
            c.Value.Status.Equals("degraded", StringComparison.OrdinalIgnoreCase));
    }
}
