using SaveState.Core.AiGaming.Entities;

namespace SaveState.Core.AiGaming.Services;

public interface ICheatDetectionService
{
    Task<CheatDetectionResult> AnalyzeMemoryAsync(
        MemorySnapshot snapshot,
        IEnumerable<long> addresses,
        CancellationToken ct = default);

    Task TrainAnomalyDetectorAsync(
        IEnumerable<MemorySnapshot> baseline,
        CancellationToken ct = default);
}
