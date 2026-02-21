using System.Text.Json.Serialization;

namespace SaveState.Infrastructure.HealthChecks;

/// <summary>
/// Standard health check response model.
/// </summary>
public class HealthCheckResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "unknown";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("durationMs")]
    public long DurationMs { get; set; }

    [JsonPropertyName("checks")]
    public Dictionary<string, HealthCheckItem> Checks { get; set; } = new();
}

/// <summary>
/// Individual health check item.
/// </summary>
public class HealthCheckItem
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "unknown";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("durationMs")]
    public long DurationMs { get; set; }

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Health status constants.
/// </summary>
public static class HealthStatus
{
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Unhealthy = "unhealthy";
}
