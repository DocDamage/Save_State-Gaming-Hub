using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting ECU (Electronic Control Unit) temperature in driving/racing games.
/// ECU temperature values typically:
/// - Are floats or integers (Celsius or Fahrenheit)
/// - Range from ambient to operating temp (40-85°C typical)
/// - Lower than engine temperature
/// - Increase with engine bay heat
/// </summary>
public sealed class EcuTempHeuristic : IValueHeuristic
{
    public string Name => "ECU Temperature Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasOperatingTemp = false;
        bool hasGradualIncrease = false;

        // Check value range
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            // Celsius range: -20 to 100
            if (currentVal.Value >= -20 && currentVal.Value <= 100)
            {
                score += 0.3;
            }
            // Fahrenheit range: -4 to 212
            else if (currentVal.Value > 100 && currentVal.Value <= 212)
            {
                score += 0.3;
            }

            // Check for operating temperature
            if ((currentVal.Value >= 40 && currentVal.Value <= 85) ||
                (currentVal.Value >= 104 && currentVal.Value <= 185))
            {
                hasOperatingTemp = true;
                score += 0.15;
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

            // Gradual warmup like engine
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                if (delta > 0 && delta < 10)
                {
                    hasGradualIncrease = true;
                    score += 0.1;
                }
            }

            // Increases with engine load
            if (curr.RelatedAction == PlayerAction.Sprinted && currVal > prevVal)
            {
                score += 0.1;
            }

            // ECU temp is typically lower than engine temp
            if (currentVal.Value >= 20 && currentVal.Value <= 90)
            {
                score += 0.05;
            }

            // Should not exceed limits
            if ((currentVal.Value > 150) || (currentVal.Value > 300)) // C or F
            {
                score -= 0.4;
            }
        }

        // Bonus for operating temp
        if (hasOperatingTemp)
            score += 0.15;

        // Bonus for gradual increase
        if (hasGradualIncrease && history.Count > 3)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}