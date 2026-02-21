using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting AWD (All-Wheel Drive) status in driving/racing games.
/// AWD status values typically:
/// - Are booleans (0/1) or percentages (0-100)
/// - Indicate front/rear torque split
/// - Change dynamically based on traction
/// - Found in performance and off-road vehicles
/// </summary>
public sealed class AwdStatusHeuristic : IValueHeuristic
{
    public string Name => "AWD Status Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasDynamicChange = false;
        bool hasBooleanPattern = false;
        bool hasPercentagePattern = false;

        // Check value range
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            // Boolean pattern (0 or 1)
            if (currentVal.Value == 0 || currentVal.Value == 1)
            {
                score += 0.3;
                hasBooleanPattern = true;
            }
            // Percentage pattern (0-100)
            else if (currentVal.Value >= 0 && currentVal.Value <= 100)
            {
                score += 0.3;
                hasPercentagePattern = true;
            }
            // Torque split pattern (e.g., 50 for 50:50)
            else if (currentVal.Value >= 0 && currentVal.Value <= 100)
            {
                score += 0.25;
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

            // Check for dynamic changes (traction-based)
            if (prevVal.Value != currVal.Value)
            {
                hasDynamicChange = true;
                score += 0.1;

                // AWD often engages during acceleration or slippery conditions
                if (curr.RelatedAction == PlayerAction.Sprinted)
                {
                    score += 0.1;
                }
            }

            // Check for 50:50 split (common AWD default)
            if (Math.Abs(currVal.Value - 50) < 5)
            {
                score += 0.1;
            }

            // Should be non-negative
            if (currVal.Value < 0)
            {
                score -= 0.4;
            }

            // Should not exceed 100
            if (currVal.Value > 100)
            {
                score -= 0.3;
            }
        }

        // Bonus for dynamic changes (characteristic of modern AWD)
        if (hasDynamicChange && history.Count > 3)
            score += 0.2;

        // Bonus for boolean pattern (simpler AWD on/off)
        if (hasBooleanPattern)
            score += 0.15;

        // Bonus for percentage pattern (torque split)
        if (hasPercentagePattern)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}