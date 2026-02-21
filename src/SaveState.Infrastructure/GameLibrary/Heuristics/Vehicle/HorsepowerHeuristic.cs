using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting engine horsepower in driving/racing games.
/// Horsepower values typically:
/// - Are static values or slowly changing (upgrades)
/// - Range from 50 (economy cars) to 2000+ (hypercars)
/// - Represent peak power output
/// - Remain constant during gameplay
/// </summary>
public sealed class HorsepowerHeuristic : IValueHeuristic
{
    public string Name => "Engine Horsepower Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isStable = true;
        bool inRealisticRange = false;

        // Check value range (HP: 20-3000)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 20 && currentVal.Value <= 3000)
        {
            score += 0.4;
            inRealisticRange = true;
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

            // Horsepower is typically stable (may change with upgrades)
            if (!HeuristicUtilities.AreValuesEqual(prevVal.Value, currVal.Value))
            {
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                // Small changes possible (tuning), large jumps suspicious
                if (delta > 100 && i > 1)
                {
                    isStable = false;
                }
            }

            // Check for common HP values
            var commonHP = new[] { 100.0, 150.0, 200.0, 250.0, 300.0, 400.0, 500.0, 600.0, 700.0, 800.0, 900.0, 1000.0 };
            foreach (var hp in commonHP)
            {
                if (Math.Abs(currVal.Value - hp) < 10)
                {
                    score += 0.05;
                    break;
                }
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for stable value (characteristic of HP stat)
        if (isStable && history.Count > 3)
            score += 0.25;

        // Bonus for realistic range
        if (inRealisticRange)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}