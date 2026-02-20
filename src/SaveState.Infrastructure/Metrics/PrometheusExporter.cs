using Microsoft.Extensions.Logging;
using SaveState.Core.Metrics;
using System.Text.Json;

namespace SaveState.Infrastructure.Metrics;

/// <summary>
/// Request context for metrics export.
/// Abstracts the HTTP request/response model for use in desktop applications.
/// </summary>
public class MetricsRequestContext
{
    public string Path { get; set; } = "/metrics";
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> QueryParameters { get; set; } = new();
}

/// <summary>
/// Response context for metrics export.
/// </summary>
public class MetricsResponseContext
{
    public int StatusCode { get; set; } = 200;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string ContentType { get; set; } = "text/plain";
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// Prometheus metrics exporter for HTTP endpoints.
/// Provides /metrics endpoint for Prometheus scraping and /metrics/snapshot for JSON snapshots.
/// Designed to work with embedded HTTP servers in desktop applications.
/// </summary>
public class PrometheusExporter
{
    private readonly IMetricsReporter _metricsReporter;
    private readonly ILogger<PrometheusExporter> _logger;

    public PrometheusExporter(IMetricsReporter metricsReporter, ILogger<PrometheusExporter> logger)
    {
        _metricsReporter = metricsReporter;
        _logger = logger;
    }

    /// <summary>
    /// Handles metrics requests.
    /// Supports:
    /// - GET /metrics - Returns Prometheus format metrics
    /// - GET /metrics/snapshot - Returns JSON snapshot of current metrics
    /// </summary>
    public async Task<MetricsResponseContext> HandleRequestAsync(MetricsRequestContext request)
    {
        var path = request.Path.TrimEnd('/');

        try
        {
            if (path.EndsWith("/metrics", StringComparison.OrdinalIgnoreCase))
            {
                return await ServePrometheusMetricsAsync();
            }
            else if (path.EndsWith("/metrics/snapshot", StringComparison.OrdinalIgnoreCase))
            {
                return await ServeMetricsSnapshotAsync();
            }
            else if (path.EndsWith("/metrics/health", StringComparison.OrdinalIgnoreCase))
            {
                return await ServeHealthCheckAsync();
            }
            else
            {
                return new MetricsResponseContext
                {
                    StatusCode = 404,
                    ContentType = "text/plain",
                    Body = "Not found. Available endpoints: /metrics, /metrics/snapshot, /metrics/health"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving metrics request for path {Path}", path);
            return new MetricsResponseContext
            {
                StatusCode = 500,
                ContentType = "text/plain",
                Body = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Exports metrics in Prometheus format for direct use.
    /// </summary>
    public string ExportPrometheusFormat()
    {
        return _metricsReporter.ExportPrometheusFormat();
    }

    /// <summary>
    /// Gets a JSON snapshot of current metrics.
    /// </summary>
    public string GetMetricsSnapshotJson()
    {
        var snapshot = _metricsReporter.GetSnapshot();
        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    private Task<MetricsResponseContext> ServePrometheusMetricsAsync()
    {
        _logger.LogDebug("Serving Prometheus metrics");

        var metrics = _metricsReporter.ExportPrometheusFormat();

        var response = new MetricsResponseContext
        {
            StatusCode = 200,
            ContentType = "text/plain; version=0.0.4; charset=utf-8",
            Headers = new Dictionary<string, string>
            {
                ["Cache-Control"] = "no-cache"
            },
            Body = metrics
        };

        _logger.LogDebug("Prometheus metrics served successfully");
        return Task.FromResult(response);
    }

    private Task<MetricsResponseContext> ServeMetricsSnapshotAsync()
    {
        _logger.LogDebug("Serving metrics snapshot");

        var snapshot = _metricsReporter.GetSnapshot();
        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        var response = new MetricsResponseContext
        {
            StatusCode = 200,
            ContentType = "application/json; charset=utf-8",
            Headers = new Dictionary<string, string>
            {
                ["Cache-Control"] = "no-cache"
            },
            Body = json
        };

        _logger.LogDebug("Metrics snapshot served successfully");
        return Task.FromResult(response);
    }

    private Task<MetricsResponseContext> ServeHealthCheckAsync()
    {
        _logger.LogDebug("Serving metrics health check");

        var snapshot = _metricsReporter.GetSnapshot();
        var isHealthy = snapshot.Counters.GetValueOrDefault("errors.total", 0) < 100;

        var health = new
        {
            Status = isHealthy ? "healthy" : "degraded",
            Timestamp = DateTime.UtcNow,
            Metrics = new
            {
                ActiveSessions = snapshot.Gauges.GetValueOrDefault("sessions.active", 0),
                AttachedProcesses = snapshot.Gauges.GetValueOrDefault("processes.attached", 0),
                TotalErrors = snapshot.Counters.GetValueOrDefault("errors.total", 0),
                TotalWarnings = snapshot.Counters.GetValueOrDefault("warnings.total", 0)
            }
        };

        var json = JsonSerializer.Serialize(health, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        var response = new MetricsResponseContext
        {
            StatusCode = isHealthy ? 200 : 503,
            ContentType = "application/json; charset=utf-8",
            Body = json
        };

        return Task.FromResult(response);
    }
}
