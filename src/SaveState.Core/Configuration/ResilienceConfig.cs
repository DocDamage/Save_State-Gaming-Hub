using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Configuration;

public class ResilienceConfig
{
    public const string Section = "Resilience";

    [Range(1, 100, ErrorMessage = "CircuitBreakerThreshold must be between 1 and 100")]
    public int CircuitBreakerThreshold { get; set; } = 5;

    [Range(1000, 300000, ErrorMessage = "CircuitBreakerDurationMs must be between 1000 and 300000 ms")]
    public int CircuitBreakerDurationMs { get; set; } = 60000;

    [Range(0, 10, ErrorMessage = "MaxRetries must be between 0 and 10")]
    public int MaxRetries { get; set; } = 3;

    [Range(100, 10000, ErrorMessage = "InitialRetryDelayMs must be between 100 and 10000 ms")]
    public int InitialRetryDelayMs { get; set; } = 1000;

    [Range(1.0, 5.0, ErrorMessage = "RetryBackoffMultiplier must be between 1.0 and 5.0")]
    public double RetryBackoffMultiplier { get; set; } = 2.0;

    [Range(5000, 120000, ErrorMessage = "DefaultTimeoutMs must be between 5000 and 120000 ms")]
    public int DefaultTimeoutMs { get; set; } = 30000;
}
