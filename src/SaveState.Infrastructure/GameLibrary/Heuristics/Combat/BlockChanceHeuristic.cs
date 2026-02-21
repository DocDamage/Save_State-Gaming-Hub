using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting block/parry chance in melee combat games.
/// Block chance values typically:
/// - Are floats (0.0-100.0) representing percentage
/// - Soft-capped around 50-75%
/// - Affected by shield/weapon stats
/// </summary>
public sealed class BlockChanceHeuristic : IValueHeuristic
{
    public string Name => "Block/Parry Chance Detection";
    public string Category => "Combat";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range (block typically 0-100%)
        if (IsInBlockRange(value.CurrentValue))
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

            // Common block values
            var commonBlocks = new[] { 10.0, 15.0, 20.0, 25.0, 30.0, 40.0, 50.0, 60.0, 75.0 };
            foreach (var block in commonBlocks)
            {
                if (Math.Abs(currVal.Value - block) < 2)
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

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsInBlockRange(object? value)
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