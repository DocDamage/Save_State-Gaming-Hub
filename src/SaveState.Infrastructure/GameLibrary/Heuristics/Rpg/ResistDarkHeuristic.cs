using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting dark/shadow resistance in RPG games.
/// Dark resist values typically:
/// - Are integers or floats (0-100) representing percentage
/// - Relatively stable, change with gear or dark-related buffs
/// - Common in dark fantasy and horror RPGs
/// </summary>
public sealed class ResistDarkHeuristic : IValueHeuristic
{
    public string Name => "Dark Resistance Detection";
    public string Category => "RPG";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool stableValue = true;

        // Check value range (resist typically 0-100%)
        if (IsInResistRange(value.CurrentValue))
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

            // Check for stability
            if (Math.Abs(currVal.Value - prevVal.Value) > 20)
            {
                stableValue = false;
            }

            // Common resistance values
            var commonResists = new[] { 0.0, 10.0, 15.0, 20.0, 25.0, 30.0, 50.0, 75.0, 100.0 };
            foreach (var resist in commonResists)
            {
                if (Math.Abs(currVal.Value - resist) < 2)
                {
                    score += 0.15;
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

        // Bonus for stability
        if (stableValue && history.Count > 2)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInResistRange(object? value)
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