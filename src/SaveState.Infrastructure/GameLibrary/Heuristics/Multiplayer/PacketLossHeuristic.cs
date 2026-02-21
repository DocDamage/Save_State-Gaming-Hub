using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting network packet loss percentage in multiplayer games.
/// Packet loss values typically:
/// - Are integers or floats (0-100%)
/// - Stay at 0 in ideal conditions
/// - Spike during network congestion
/// - Rarely exceed 10% in normal gameplay
/// </summary>
public sealed class PacketLossHeuristic : IValueHeuristic
{
    public string Name => "Packet Loss Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool mostlyZero = true;
        int spikeEvents = 0;

        // Check value range (packet loss 0-100%)
        if (IsInPacketLossRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Prefer numeric types
        if (HeuristicUtilities.IsNonNegative(value.CurrentValue))
        {
            score += 0.1;
        }

        // Analyze observation history
        for (int i = 1; i < history.Count; i++)
        {
            var prev = history[i - 1];
            var curr = history[i];

            if (prev.Value == null || curr.Value == null)
                continue;

            double? prevVal = HeuristicUtilities.ConvertToDouble(prev.Value);
            double? currVal = HeuristicUtilities.ConvertToDouble(curr.Value);

            if (!prevVal.HasValue || !currVal.HasValue)
                continue;

            // Check if mostly zero (ideal network)
            if (currVal > 0)
            {
                mostlyZero = false;
            }

            // Detect spikes (sudden increase)
            if (currVal > prevVal * 2 && currVal > 1)
            {
                spikeEvents++;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Should not exceed 100%
            if (currVal > 100)
            {
                score -= 0.4;
            }

            // Normal range bonus
            if (currVal <= 10)
            {
                score += 0.05;
            }
        }

        // Bonus for mostly zero (common case)
        if (mostlyZero && history.Count > 2)
            score += 0.2;

        // Bonus for occasional spikes (realistic behavior)
        if (spikeEvents >= 1 && spikeEvents <= 5)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "float" or "single" or "double";
    }

    private static bool IsInPacketLossRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}