using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting energy cells/batteries in sci-fi games.
/// Energy cell values typically:
/// - Are integers (0-100) or percentage
/// - Deplete when using high-tech equipment
/// - Recharge at stations
/// </summary>
public sealed class EnergyCellHeuristic : IValueHeuristic
{
    public string Name => "Energy Cell/Battery Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int depletionEvents = 0;
        int rechargeEvents = 0;

        // Check value range (energy typically 0-100 or 0-1000)
        if (IsInEnergyRange(value.CurrentValue))
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

            // Check for depletion (using equipment)
            if (currVal < prevVal)
            {
                depletionEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Equipment drains energy
                if (delta > 0 && delta < 50)
                {
                    score += 0.12;
                }
            }

            // Check for recharge (at station)
            if (currVal > prevVal)
            {
                rechargeEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Recharge restores significant amount
                if (delta > 20)
                {
                    score += 0.15;
                }
            }

            // Should not go negative
            if (currVal < 0)
            {
                score -= 0.5;
            }

            // Typically caps at 100 or 1000
            if (currVal > 2000)
            {
                score -= 0.3;
            }
        }

        // Bonus for patterns
        if (depletionEvents >= 2)
            score += 0.15;
        if (rechargeEvents >= 1)
            score += 0.1;

        // Check for max of 100 or 1000
        var maxValue = history
            .Where(o => o.Value != null)
            .Select(o => HeuristicUtilities.ConvertToDouble(o.Value))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .Max();

        if (Math.Abs(maxValue - 100) < 5 || Math.Abs(maxValue - 1000) < 50)
        {
            score += 0.1;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "int32" or "int" or "double";
    }

    private static bool IsInEnergyRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 5000;
        }
        catch
        {
            return false;
        }
    }
}