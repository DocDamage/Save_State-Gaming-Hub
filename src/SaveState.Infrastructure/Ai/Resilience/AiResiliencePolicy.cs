using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using SaveState.Core.Configuration;

namespace SaveState.Infrastructure.Ai.Resilience;

public interface IAiResiliencePolicy
{
    AsyncPolicyWrap GetPipelinePolicy(string providerName);
}

public class AiResiliencePolicy : IAiResiliencePolicy
{
    private readonly ResilienceConfig _config;
    private readonly ILogger<AiResiliencePolicy> _logger;

    public AiResiliencePolicy(
        IOptions<ResilienceConfig> config,
        ILogger<AiResiliencePolicy> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public virtual AsyncPolicyWrap GetPipelinePolicy(string providerName)
    {
        var circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: _config.CircuitBreakerThreshold,
                durationOfBreak: TimeSpan.FromMilliseconds(_config.CircuitBreakerDurationMs),
                onBreak: (ex, breakDelay) =>
                {
                    _logger.LogWarning("Circuit breaker opened for {Provider}", providerName);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset for {Provider}", providerName);
                });

        var retry = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: _config.MaxRetries,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(_config.InitialRetryDelayMs *
                        Math.Pow(_config.RetryBackoffMultiplier, attempt)),
                onRetry: (ex, delay, attempt, context) =>
                {
                    _logger.LogWarning(ex, "Retry {Attempt} for {Provider}", attempt, providerName);
                });

        var timeout = Policy.TimeoutAsync(
            TimeSpan.FromMilliseconds(_config.DefaultTimeoutMs),
            TimeoutStrategy.Pessimistic);

        return Policy.WrapAsync(timeout, retry, circuitBreaker);
    }
}
