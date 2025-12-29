namespace SaveState.Core.Ai.Services;

public interface IChaosTester
{
    Task<ChaosTestResult> RunCircuitBreakerChaosTestAsync(int testDurationSeconds = 30, CancellationToken ct = default);
}

public record ChaosTestResult(
    bool TestPassed,
    int RequestsSent,
    int FailuresInduced,
    int CircuitBreakerOpened,
    int FallbacksUsed,
    string Summary);
