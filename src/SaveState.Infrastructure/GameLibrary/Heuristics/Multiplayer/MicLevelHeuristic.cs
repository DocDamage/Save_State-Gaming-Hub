using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting microphone input level in multiplayer games.
/// Mic level values typically:
/// - Are integers (0-100) or floats
/// - Fluctuate rapidly when speaking
/// - Stay near 0 when silent
/// - React to audio input
/// </summary>
public sealed class MicLevelHeuristic : IValueHeuristic
{
    public string Name => "Microphone Level Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFluctuation = false;
        bool inValidRange = true;
        bool goesToZero = false;

        // Check value range (mic level typically 0-100)
        if (IsInMicLevelRange(value.CurrentValue))
        {
            score += 0.35;
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

            // Check for fluctuation (mic levels change constantly)
            if (!HeuristicUtilities.AreValuesEqual(currVal.Value, prevVal.Value))
            {
                hasFluctuation = true;
            }

            // Check if it goes to zero (silence)
            if (currVal == 0)
            {
                goesToZero = true;
            }

            // Check for valid range
            if (currVal < 0 || currVal > 100)
            {
                inValidRange = false;
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Should not exceed 100
            if (currVal > 100)
            {
                score -= 0.4;
            }
        }

        // Strong bonus for fluctuation (key characteristic)
        if (hasFluctuation)
            score += 0.3;

        // Bonus for going to zero (silence periods)
        if (goesToZero)
            score += 0.15;

        // Bonus for valid range
        if (inValidRange && history.Count > 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte" or "float" or "single" or "double";
    }

    private static bool IsInMicLevelRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Mic level is typically 0-100
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}