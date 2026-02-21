using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting engine torque in driving/racing games.
/// Torque values typically:
/// - Are static values or slowly changing (upgrades)
/// - Range from 50 lb-ft (economy cars) to 2000+ (heavy trucks/supercars)
/// - Represent peak torque output
/// - Usually displayed in Nm or lb-ft
/// </summary>
public sealed class TorqueHeuristic : IValueHeuristic
{
    public string Name => "Engine Torque Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isStable = true;

        // Check value range (Torque: 20-2500 Nm or lb-ft)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 20 && currentVal.Value <= 2500)
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

            // Torque is typically stable like HP
            if (!HeuristicUtilities.AreValuesEqual(prevVal.Value, currVal.Value))
            {
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                if (delta > 50 && i > 1)
                {
                    isStable = false;
                }
            }

            // Check for common torque values
            var commonTorque = new[] { 100.0, 150.0, 200.0, 250.0, 300.0, 400.0, 500.0, 600.0, 700.0, 800.0 };
            foreach (var tq in commonTorque)
            {
                if (Math.Abs(currVal.Value - tq) < 10)
                {
                    score += 0.05;
                    break;
                }
            }

            // Check for typical ratio to horsepower (torque is usually similar or slightly higher)
            // This is a weak signal but useful
            if (currVal.Value >= 50 && currVal.Value <= 1500)
            {
                score += 0.05;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for stable value
        if (isStable && history.Count > 3)
            score += 0.25;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}