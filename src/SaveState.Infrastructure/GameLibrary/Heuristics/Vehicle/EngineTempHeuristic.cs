using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting engine temperature in driving/racing games.
/// Engine temperature values typically:
/// - Are floats or integers (Celsius or Fahrenheit)
/// - Range from cold start (ambient) to operating temp (80-100°C or 176-212°F)
/// - Increase gradually from start
/// - Spike during heavy load or low coolant
/// </summary>
public sealed class EngineTempHeuristic : IValueHeuristic
{
    public string Name => "Engine Temperature Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasGradualWarmup = false;
        bool hasOperatingTemp = false;
        bool isCelsius = false;
        bool isFahrenheit = false;

        // Check value range
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            // Celsius range: -40 to 150
            if (currentVal.Value >= -40 && currentVal.Value <= 150)
            {
                score += 0.25;
                isCelsius = true;
            }
            // Fahrenheit range: -40 to 300
            else if (currentVal.Value > 150 && currentVal.Value <= 300)
            {
                score += 0.25;
                isFahrenheit = true;
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

            // Check for operating temperature (80-100°C or 176-212°F)
            if ((currVal.Value >= 80 && currVal.Value <= 100) ||
                (currVal.Value >= 176 && currVal.Value <= 212))
            {
                hasOperatingTemp = true;
                score += 0.1;
            }

            // Check for gradual warmup
            if (currVal > prevVal)
            {
                var delta = currVal.Value - prevVal.Value;
                if (delta > 0 && delta < 10) // Gradual increase
                {
                    hasGradualWarmup = true;
                    score += 0.05;
                }
            }

            // Temperature spikes during heavy action
            if (curr.RelatedAction == PlayerAction.Sprinted && currVal > prevVal)
            {
                score += 0.05;
            }

            // Check for overheating threshold
            if ((isCelsius && currVal.Value > 120) || (isFahrenheit && currVal.Value > 248))
            {
                score += 0.1;
            }

            // Should not exceed physical limits
            if ((isCelsius && currVal.Value > 200) || (isFahrenheit && currVal.Value > 392))
            {
                score -= 0.4;
            }
        }

        // Bonus for warmup pattern
        if (hasGradualWarmup && history.Count > 3)
            score += 0.2;

        // Bonus for operating temp detection
        if (hasOperatingTemp)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}