using SaveState.Core.AiGaming.Entities;
using SaveState.Core.Common.Services;

namespace SaveState.Core.AiGaming.Services;

public class CheatDetectionService : ICheatDetectionService
{
    private Dictionary<long, byte[]>? _baselinePatterns;
    private readonly ITimeProvider _timeProvider;

    public CheatDetectionService(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<CheatDetectionResult> AnalyzeMemoryAsync(
        MemorySnapshot snapshot,
        IEnumerable<long> addresses,
        CancellationToken ct = default)
    {
        if (_baselinePatterns is null)
        {
            return Task.FromResult(CheatDetectionResult.NoCheating(_timeProvider));
        }

        var flaggedAddresses = new List<long>();
        double totalConfidence = 0.0;
        var analysisCount = 0;

        foreach (var address in addresses)
        {
            if (_baselinePatterns.TryGetValue(address, out var baselineData))
            {
                // Simple pattern matching - compare memory values
                var confidence = CalculateSimilarity(snapshot.Data, baselineData);
                totalConfidence += confidence;
                analysisCount++;

                // Flag addresses with low similarity (potential cheating)
                if (confidence < 0.8) // Threshold for suspicious activity
                {
                    flaggedAddresses.Add(address);
                }
            }
        }

        if (flaggedAddresses.Any())
        {
            var averageConfidence = analysisCount > 0 ? totalConfidence / analysisCount : 0.0;
            var cheatingConfidence = 1.0 - averageConfidence; // Invert confidence for cheating likelihood

            return Task.FromResult(CheatDetectionResult.CheatingDetected(
                cheatingConfidence,
                "Pattern Analysis",
                flaggedAddresses,
                $"Memory patterns differ significantly from baseline at {flaggedAddresses.Count} addresses",
                _timeProvider
            ));
        }

        return Task.FromResult(CheatDetectionResult.NoCheating(_timeProvider));
    }

    public Task TrainAnomalyDetectorAsync(
        IEnumerable<MemorySnapshot> baseline,
        CancellationToken ct = default)
    {
        _baselinePatterns = new Dictionary<long, byte[]>();

        // Aggregate baseline data by address
        var addressGroups = baseline.GroupBy(s => s.Address);

        foreach (var group in addressGroups)
        {
            // Use the most recent snapshot as baseline for each address
            var latestSnapshot = group.OrderByDescending(s => s.CapturedAt).First();
            _baselinePatterns[group.Key] = latestSnapshot.Data;
        }

        return Task.CompletedTask;
    }

    private static double CalculateSimilarity(byte[] current, byte[] baseline)
    {
        if (current.Length != baseline.Length)
        {
            return 0.0; // Complete mismatch if different sizes
        }

        var differences = 0;
        for (var i = 0; i < current.Length; i++)
        {
            if (current[i] != baseline[i])
            {
                differences++;
            }
        }

        // Return similarity as percentage of matching bytes
        return 1.0 - (differences / (double)current.Length);
    }
}
