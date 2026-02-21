using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting voice chat volume level in multiplayer games.
/// Voice chat volume values typically:
/// - Are integers or floats (0-100 or 0-1.0)
/// - Set by user preferences
/// - Stay relatively constant
/// - Change only when adjusted by user
/// </summary>
public sealed class VoiceChatVolumeHeuristic : IValueHeuristic
{
    public string Name => "Voice Chat Volume Detection";
    public string Category => "Multiplayer";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isConstant = true;
        bool inValidRange = true;

        // Check value range (volume typically 0-100 or 0-1.0)
        if (IsInVolumeRange(value.CurrentValue))
        {
            score += 0.4;
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

            // Check for constancy (volume is preference-based)
            if (!HeuristicUtilities.AreValuesEqual(currVal.Value, prevVal.Value))
            {
                isConstant = false;
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

        // Bonus for being constant (user setting)
        if (isConstant && history.Count > 2)
            score += 0.25;

        // Bonus for valid range
        if (inValidRange && history.Count > 1)
            score += 0.15;

        // Check for common volume values
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            var commonVolumes = new[] { 0.0, 10.0, 25.0, 50.0, 75.0, 80.0, 90.0, 100.0 };
            foreach (var common in commonVolumes)
            {
                if (Math.Abs(currentVal.Value - common) < 2)
                {
                    score += 0.1;
                    break;
                }
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short" or "byte" or "float" or "single" or "double";
    }

    private static bool IsInVolumeRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            // Volume can be 0-100 (percentage) or 0-1.0 (normalized)
            return (val >= 0 && val <= 100) || (val >= 0 && val <= 1.0);
        }
        catch
        {
            return false;
        }
    }
}