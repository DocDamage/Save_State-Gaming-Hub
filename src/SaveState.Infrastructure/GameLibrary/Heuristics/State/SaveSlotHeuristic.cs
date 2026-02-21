using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting save slot values in game memory.
/// Save slot values typically:
/// - Are integers representing current save slot
/// - Range from 0 to 9 or 1 to 10
/// - Remain constant during gameplay
/// - Only change at save/load screens
/// </summary>
public sealed class SaveSlotHeuristic : IValueHeuristic
{
    public string Name => "Save Slot Detection";
    public string Category => "State";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;

        // Check value range for save slots
        if (IsInSaveSlotRange(value.CurrentValue))
        {
            score += 0.4;
        }

        // Should be integer
        if (HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score += 0.25;
        }

        // Non-negative
        if (HeuristicUtilities.IsNonNegative(value.CurrentValue))
        {
            score += 0.1;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            int constants = 0;
            int changes = 0;
            double? lastValue = null;

            for (int i = 0; i < history.Count; i++)
            {
                var obs = history[i];
                if (obs.Value == null)
                    continue;

                var currVal = HeuristicUtilities.ConvertToDouble(obs.Value);
                if (!currVal.HasValue)
                    continue;

                if (lastValue.HasValue)
                {
                    if (currVal.Value == lastValue.Value)
                    {
                        constants++;
                    }
                    else
                    {
                        changes++;
                    }
                }

                lastValue = currVal.Value;
            }

            // Save slot should remain constant for very long periods
            var totalComparisons = history.Count - 1;
            if (totalComparisons > 0)
            {
                var constantRatio = (double)constants / totalComparisons;
                if (constantRatio > 0.95)
                {
                    score += 0.25;
                }
                else if (constantRatio > 0.8)
                {
                    score += 0.1;
                }
            }

            // Changes are very rare
            if (changes == 0)
            {
                score += 0.1;
            }
            else if (changes == 1)
            {
                score += 0.05;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int64" or "long" or "byte" or "uint8";
    }

    private static bool IsInSaveSlotRange(object? value)
    {
        if (value == null) return false;

        var doubleValue = HeuristicUtilities.ConvertToDouble(value);
        if (!doubleValue.HasValue) return false;

        var val = doubleValue.Value;
        // Save slots typically 0-9 or 1-10
        return val >= 0 && val <= 10;
    }
}