using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using SaveState.Core.Configuration;

namespace SaveState.Infrastructure.Resilience;

/// <summary>
/// Provides resilience policies for HTTP clients using Microsoft.Extensions.Http.Resilience.
/// Includes retry with exponential backoff, circuit breaker, and timeout policies.
/// </summary>
public static class HttpResiliencePolicyHandler
{
    /// <summary>
    /// Adds standard resilience policies (retry, circuit breaker, timeout) to an HTTP client builder.
    /// Uses configuration from ResilienceConfig for policy parameters.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="clientName">Name of the client for logging purposes.</param>
    /// <returns>The configured HTTP client builder.</returns>
    public static IHttpClientBuilder AddResiliencePolicies(
        this IHttpClientBuilder builder,
        string clientName)
    {
        builder.AddResilienceHandler($"{clientName}-resilience", (resilienceBuilder, context) =>
        {
            var config = context.ServiceProvider.GetRequiredService<IOptions<ResilienceConfig>>().Value;

            // Add retry policy with exponential backoff
            resilienceBuilder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = config.MaxRetries,
                Delay = TimeSpan.FromMilliseconds(config.InitialRetryDelayMs),
                BackoffType = Polly.DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = static args => ValueTask.FromResult(HttpClientResiliencePredicates.IsTransient(args.Outcome))
            });

            // Add circuit breaker policy
            resilienceBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = config.CircuitBreakerThreshold,
                BreakDuration = TimeSpan.FromMilliseconds(config.CircuitBreakerDurationMs),
                ShouldHandle = static args => ValueTask.FromResult(HttpClientResiliencePredicates.IsTransient(args.Outcome))
            });

            // Add timeout policy
            resilienceBuilder.AddTimeout(TimeSpan.FromMilliseconds(config.DefaultTimeoutMs));
        });

        return builder;
    }
}
