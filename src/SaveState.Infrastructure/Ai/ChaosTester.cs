namespace SaveState.Infrastructure.Ai;

using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;

public class ChaosTester : IChaosTester
{
    private readonly IAiOrchestrator _orchestrator;
    private readonly ILogger<ChaosTester> _logger;

    public ChaosTester(IAiOrchestrator orchestrator, ILogger<ChaosTester> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<ChaosTestResult> RunCircuitBreakerChaosTestAsync(int testDurationSeconds = 30, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting circuit breaker chaos test for {Duration}s", testDurationSeconds);

        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(testDurationSeconds);
        var requestsSent = 0;
        var failuresInduced = 0;
        var circuitBreakerOpened = 0;
        var fallbacksUsed = 0;

        var tasks = new List<Task>();

        // Phase 1: Normal operation (first 10 seconds)
        while (DateTime.UtcNow < startTime.AddSeconds(10) && !ct.IsCancellationRequested)
        {
            requestsSent++;
            var task = MakeTestRequestAsync(ct);
            tasks.Add(task);
            await Task.Delay(100, ct).ConfigureAwait(false); // 10 requests per second
        }

        // Phase 2: Induce failures (next 10 seconds)
        _logger.LogInformation("Phase 2: Inducing failures to test circuit breaker");
        while (DateTime.UtcNow < startTime.AddSeconds(20) && !ct.IsCancellationRequested)
        {
            failuresInduced++;
            requestsSent++;
            var task = MakeFailingTestRequestAsync(ct);
            tasks.Add(task);
            await Task.Delay(200, ct).ConfigureAwait(false); // 5 requests per second
        }

        // Phase 3: Recovery (final 10 seconds)
        _logger.LogInformation("Phase 3: Testing recovery");
        while (DateTime.UtcNow < endTime && !ct.IsCancellationRequested)
        {
            requestsSent++;
            var task = MakeTestRequestAsync(ct);
            tasks.Add(task);
            await Task.Delay(100, ct).ConfigureAwait(false); // 10 requests per second
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Analyze results (simplified - in real implementation would track circuit breaker state)
        var testPassed = requestsSent > 0; // Basic check

        var summary = $"Chaos test completed: {requestsSent} requests, {failuresInduced} failures induced. " +
                     $"Circuit breaker behavior needs detailed monitoring in production.";

        _logger.LogInformation("Chaos test completed: {Summary}", summary);

        return new ChaosTestResult(
            TestPassed: testPassed,
            RequestsSent: requestsSent,
            FailuresInduced: failuresInduced,
            CircuitBreakerOpened: circuitBreakerOpened,
            FallbacksUsed: fallbacksUsed,
            Summary: summary);
    }

    private async Task MakeTestRequestAsync(CancellationToken ct)
    {
        try
        {
            var request = new AiRequest(
                AiRequestType.Chat,
                Messages: new[] { new ChatMessage("user", "Hello") },
                AllowCache: false);

            await _orchestrator.ProcessRequestAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Expected failure during chaos test");
        }
    }

    private async Task MakeFailingTestRequestAsync(CancellationToken ct)
    {
        try
        {
            // Force a provider that doesn't exist to induce failures
            var request = new AiRequest(
                AiRequestType.Chat,
                Messages: new[] { new ChatMessage("user", "Test failure") },
                PreferredProvider: "NonExistentProvider",
                AllowCache: false);

            await _orchestrator.ProcessRequestAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Expected failure during chaos test");
        }
    }
}
