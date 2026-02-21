using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting dodge/evasion chance in RPG games.
/// Dodge values typically:
/// - Are floats (0.0-100.0) representing percentage
/// - Soft-capped around 50-75%
/// - Affected by agility/gear
/// </summary>
public sealed class DodgeChanceHeuristic : IValueHeuristic
{
    public string Name => "Dodge/Evasion Chance Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool inEvasionRange = true;

        // Check value range (dodge typically 0-100%)
        if (IsInDodgeRange(value.CurrentValue))
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

            // Check for reasonable range
            if (currVal > 75)
            {
                inEvasionRange = false;
            }

            // Common dodge values
            var commonDodge = new[] { 5.0, 10.0, 15.0, 20.0, 25.0, 30.0, 35.0, 40.0, 50.0 };
            foreach (var dodge in commonDodge)
            {
                if (Math.Abs(currVal.Value - dodge) < 2)
                {
                    score += 0.12;
                    break;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Hard cap at 100%
            if (currVal > 100)
            {
                score -= 0.5;
            }
        }

        // Bonus for being in typical evasion range
        if (inEvasionRange)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInDodgeRange(object? value)
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