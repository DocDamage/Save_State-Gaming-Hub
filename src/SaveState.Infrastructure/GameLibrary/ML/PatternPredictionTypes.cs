using System.Diagnostics;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.ML;

/// <summary>
/// Profile for a specific game genre containing learned patterns.
/// </summary>
public sealed class GenrePatternProfile
{
    public GameGenre Genre { get; set; }
    public List<PatternFrequency> CommonPatterns { get; set; } = new();
    public int TotalDiscoveries { get; set; }

    public void AddDiscovery(SuccessfulDiscovery discovery)
    {
        TotalDiscoveries++;

        var existing = CommonPatterns
            .FirstOrDefault(p => p.PatternType.Equals(discovery.PatternType, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new PatternFrequency
            {
                PatternType = discovery.PatternType,
                SuggestedValueType = discovery.ValueType
            };
            CommonPatterns.Add(existing);
        }

        existing.Count++;
        existing.CommonOffsets.Add(discovery.RelativeAddress);
        existing.UpdateSuccessRate();
    }
}

/// <summary>
/// Profile for a specific game engine containing learned patterns.
/// </summary>
public sealed class EnginePatternProfile
{
    public GameEngine Engine { get; set; }
    public List<PatternFrequency> CommonPatterns { get; set; } = new();
    public int TotalDiscoveries { get; set; }

    public void AddDiscovery(SuccessfulDiscovery discovery)
    {
        TotalDiscoveries++;

        var existing = CommonPatterns
            .FirstOrDefault(p => p.PatternType.Equals(discovery.PatternType, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new PatternFrequency
            {
                PatternType = discovery.PatternType,
                SuggestedValueType = discovery.ValueType
            };
            CommonPatterns.Add(existing);
        }

        existing.Count++;
        existing.CommonOffsets.Add(discovery.RelativeAddress);
        existing.UpdateSuccessRate();
    }
}

/// <summary>
/// Frequency statistics for a specific pattern type.
/// </summary>
public sealed class PatternFrequency
{
    public string PatternType { get; set; } = string.Empty;
    public int Count { get; set; }
    public string SuggestedValueType { get; set; } = "int32";
    public List<long> CommonOffsets { get; set; } = new();
    public double SuccessRate { get; private set; }
    public double Frequency => Count / 100.0; // Normalized frequency
    public int Priority => Math.Min(Count * 10, 100);
    public double TypicalMinValue { get; set; }
    public double TypicalMaxValue { get; set; }

    public void UpdateSuccessRate()
    {
        // Placeholder for success rate calculation
        SuccessRate = 0.75; // Default assumption
    }
}
