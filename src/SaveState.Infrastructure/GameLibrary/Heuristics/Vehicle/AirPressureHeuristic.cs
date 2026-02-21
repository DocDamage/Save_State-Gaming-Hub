using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting air pressure in driving/racing games.
/// Air pressure values typically:
/// - Are floats measured in PSI, bar, or kPa
/// - Range from ambient (14.7 PSI, 1.0 bar, 101 kPa) to high boost
/// - Used for tire pressure or manifold pressure
/// - Change with altitude and temperature
/// </summary>
public sealed class AirPressureHeuristic : IValueHeuristic
{
    public string Name => "Air Pressure Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasAmbientValue = false;
        bool hasStableReading = true;

        // Check value range
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            // PSI range (tire pressure: 20-60, ambient: ~14.7)
            if (currentVal.Value >= 10 && currentVal.Value <= 100)
            {
                score += 0.25;
            }
            // Bar range (tire pressure: 1.4-4.1, ambient: ~1.0)
            else if (currentVal.Value >= 0.5 && currentVal.Value <= 7)
            {
                score += 0.25;
            }
            // kPa range (tire pressure: 140-410, ambient: ~101)
            else if (currentVal.Value >= 50 && currentVal.Value <= 700)
            {
                score += 0.25;
            }

            // Check for ambient pressure values
            if (Math.Abs(currentVal.Value - 14.7) < 2 ||
                Math.Abs(currentVal.Value - 1.0) < 0.1 ||
                Math.Abs(currentVal.Value - 101.3) < 5)
            {
                hasAmbientValue = true;
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

            // Air pressure is relatively stable
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 10)
            {
                hasStableReading = false;
            }

            // Tire pressure changes slowly with temperature
            if (delta > 0 && delta < 5)
            {
                score += 0.05;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }

            // Check for typical tire pressure values
            if ((currentVal.Value >= 28 && currentVal.Value <= 45) || // PSI
                (currentVal.Value >= 1.9 && currentVal.Value <= 3.1) || // Bar
                (currentVal.Value >= 190 && currentVal.Value <= 310)) // kPa
            {
                score += 0.1;
            }
        }

        // Bonus for ambient detection
        if (hasAmbientValue)
            score += 0.15;

        // Bonus for stable readings
        if (hasStableReading && history.Count > 3)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}