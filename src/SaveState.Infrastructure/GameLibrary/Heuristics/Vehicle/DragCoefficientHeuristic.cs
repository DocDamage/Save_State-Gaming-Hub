using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting drag coefficient (Cd) in driving/racing games.
/// Drag coefficient values typically:
/// - Are static floats (0.15-0.50 for most vehicles)
/// - Lower values for aerodynamic vehicles
/// - Remain constant per vehicle model
/// - Affect top speed and fuel efficiency
/// </summary>
public sealed class DragCoefficientHeuristic : IValueHeuristic
{
    public string Name => "Drag Coefficient Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool isStable = true;

        // Check value range (Cd: 0.15-0.60 for typical vehicles, up to 1.0 for trucks)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            if (currentVal.Value >= 0.15 && currentVal.Value <= 0.60)
            {
                score += 0.5; // Typical car range
            }
            else if (currentVal.Value >= 0.60 && currentVal.Value <= 1.0)
            {
                score += 0.35; // Truck/SUV range
            }
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

            // Cd should be very stable (only changes with mods/aero upgrades)
            if (!HeuristicUtilities.AreValuesEqual(prevVal.Value, currVal.Value))
            {
                var delta = Math.Abs(currVal.Value - prevVal.Value);
                if (delta > 0.05 && i > 1)
                {
                    isStable = false;
                }
            }

            // Check for common Cd values
            var commonCd = new[] { 0.20, 0.25, 0.30, 0.32, 0.35, 0.38, 0.40, 0.45 };
            foreach (var cd in commonCd)
            {
                if (Math.Abs(currVal.Value - cd) < 0.02)
                {
                    score += 0.1;
                    break;
                }
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }

            // Should not exceed 1.5 (even for worst vehicles)
            if (currVal.Value > 1.5)
            {
                score -= 0.4;
            }
        }

        // Bonus for stability (characteristic of Cd)
        if (isStable && history.Count > 3)
            score += 0.25;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}