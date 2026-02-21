using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting vehicle speed in racing/driving games.
/// Vehicle speed values typically:
/// - Are floats (km/h or mph with decimals)
/// - Fluctuate rapidly during driving
/// - Are 0 when stationary
/// - Have maximum limits based on vehicle
/// </summary>
public sealed class VehicleSpeedHeuristic : IValueHeuristic
{
    public string Name => "Vehicle Speed Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasFluctuation = false;
        bool canBeZero = false;
        int zeroCount = 0;
        double maxSpeed = 0;

        // Check for float type (speed usually has decimals)
        if (IsFloatType(value.ValueType))
        {
            score += 0.2;
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

            // Track max speed
            if (currVal > maxSpeed)
                maxSpeed = currVal.Value;

            // Check for zero (stationary)
            if (currVal == 0)
            {
                zeroCount++;
                canBeZero = true;
            }

            // Check for fluctuation (speed changes)
            if (Math.Abs(currVal.Value - prevVal.Value) > 0.1)
            {
                hasFluctuation = true;
            }

            // Check for reasonable speed range (0-500 km/h or mph)
            if (currVal > 500)
            {
                score -= 0.3;
            }

            // Negative speed is unusual (only in reverse, and usually shown as positive)
            if (currVal < 0)
            {
                score -= 0.2;
            }
        }

        // Bonus for zero state (vehicles can be stationary)
        if (canBeZero)
            score += 0.2;

        // Bonus for fluctuation (speed changes constantly)
        if (hasFluctuation)
            score += 0.2;

        // Bonus for reasonable max speed
        if (maxSpeed > 0 && maxSpeed <= 400)
            score += 0.15;

        // Common speed limits align with vehicle capabilities
        var commonMaxSpeeds = new[] { 120.0, 200.0, 250.0, 300.0, 350.0 };
        foreach (var commonMax in commonMaxSpeeds)
        {
            if (Math.Abs(maxSpeed - commonMax) < 20)
            {
                score += 0.1;
                break;
            }
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "float" or "single" or "double" or "int32" or "int";
    }

    private static bool IsFloatType(string valueType)
    {
        var normalized = valueType.ToLowerInvariant();
        return normalized is "float" or "single" or "double";
    }
}