namespace SaveState.Core.Configuration;

public class ResilienceConfig
{
    public const string Section = "Resilience";

    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDurationMs { get; set; } = 60000;
    public int MaxRetries { get; set; } = 3;
    public int InitialRetryDelayMs { get; set; } = 1000;
    public double RetryBackoffMultiplier { get; set; } = 2.0;
    public int DefaultTimeoutMs { get; set; } = 30000;
}
