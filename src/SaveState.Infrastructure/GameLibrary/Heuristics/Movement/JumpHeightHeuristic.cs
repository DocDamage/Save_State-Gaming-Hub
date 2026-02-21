using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting jump height/capability values in game memory.
/// Jump height values typically:
/// - Are floats in range 0.0-1000.0
/// - Have a static base value
/// - Show temporary changes during jump
/// </summary>
public sealed class JumpHeightHeuristic : IValueHeuristic
{
    public string Name => "Jump Height Detection";
    public string Category => "Movement";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        double? baseValue = null;
        int returnsToBase = 0;
        int temporaryChanges = 0;

        // Check value range
        if (IsInJumpRange(value.CurrentValue))
        {
            score += 0.3;
        }

        // Analyze observation history
        if (history.Count >= 3)
        {
            var values = history
                .Where(h => h.Value != null)
                .Select(h => HeuristicUtilities.ConvertToDouble(h.Value))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (values.Count >= 3)
            {
                // Find most common value (base value)
                baseValue = values.GroupBy(v => Math.Round(v, 2))
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Key;

                // Check for values returning to base after changes
                for (int i = 1; i < values.Count; i++)
                {
                    if (Math.Abs(values[i] - baseValue.Value) < 0.01 &&
                        Math.Abs(values[i - 1] - baseValue.Value) > 0.01)
                    {
                        returnsToBase++;
                    }

                    if (Math.Abs(values[i] - values[i - 1]) > 10 &&
                        Math.Abs(values[i] - baseValue.Value) > 0.01)
                    {
                        temporaryChanges++;
                    }
                }
            }
        }

        // Bonus for returning to base value (static base with temporary changes)
        if (returnsToBase >= 2)
        {
            score += 0.25;
        }

        // Bonus for temporary changes during jumps
        if (temporaryChanges >= 1)
        {
            score += 0.2;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double";
    }

    private static bool IsInJumpRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0.0 && val <= 1000.0;
        }
        catch
        {
            return false;
        }
    }
}
