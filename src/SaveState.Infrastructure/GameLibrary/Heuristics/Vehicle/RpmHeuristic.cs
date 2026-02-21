using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting engine RPM (revolutions per minute) in driving/racing games.
/// RPM values typically:
/// - Are integers or floats representing engine speed
/// - Range from idle (~600-1000) to redline (~6000-12000)
/// - Fluctuate with throttle input
/// - Drop to 0 when engine is off
/// </summary>
public sealed class RpmHeuristic : IValueHeuristic
{
    public string Name => "Engine RPM Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFluctuation = false;
        bool hasIdleState = false;
        bool hasRedline = false;

        // Check value range (typical RPM: 0-20000)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue && currentVal.Value >= 0 && currentVal.Value <= 20000)
        {
            score += 0.3;
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

            // Check for idle range (600-1000 RPM)
            if (currVal.Value >= 600 && currVal.Value <= 1000)
            {
                hasIdleState = true;
            }

            // Check for redline range (5000-12000 RPM)
            if (currVal.Value >= 5000 && currVal.Value <= 12000)
            {
                hasRedline = true;
            }

            // Check for fluctuations (characteristic of RPM)
            var delta = Math.Abs(currVal.Value - prevVal.Value);
            if (delta > 50 && delta < 3000)
            {
                hasFluctuation = true;
                score += 0.05;
            }

            // RPM should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.5;
            }

            // RPM typically doesn't exceed 20000 even in race cars
            if (currVal.Value > 20000)
            {
                score -= 0.4;
            }

            // RPM drops to 0 when engine off
            if (currVal.Value == 0 && prevVal.Value > 0)
            {
                score += 0.1;
            }
        }

        // Bonus for idle state detection
        if (hasIdleState)
            score += 0.2;

        // Bonus for redline detection
        if (hasRedline)
            score += 0.15;

        // Bonus for fluctuation pattern
        if (hasFluctuation && history.Count > 3)
            score += 0.15;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}