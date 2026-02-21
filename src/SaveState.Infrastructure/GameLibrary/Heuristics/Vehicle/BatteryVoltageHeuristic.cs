using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting battery voltage in driving/racing games.
/// Battery voltage values typically:
/// - Are floats (10.5-14.5 volts for 12V systems, 22-28 for 24V)
/// - Around 12.6V when engine off, 13.5-14.5V when charging
/// - Drop when cranking, stabilize when running
/// - Critical for electrical systems
/// </summary>
public sealed class BatteryVoltageHeuristic : IValueHeuristic
{
    public string Name => "Battery Voltage Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasNormalRange = false;
        bool hasChargingVoltage = false;

        // Check value range (10-30 volts)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            // 12V system
            if (currentVal.Value >= 10.5 && currentVal.Value <= 15)
            {
                score += 0.4;
                hasNormalRange = true;
            }
            // 24V system (trucks)
            else if (currentVal.Value >= 22 && currentVal.Value <= 30)
            {
                score += 0.35;
                hasNormalRange = true;
            }

            // Check for charging voltage
            if (currentVal.Value >= 13.5 && currentVal.Value <= 14.8)
            {
                hasChargingVoltage = true;
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

            // Check for voltage drop during cranking/startup
            if (currVal < prevVal && currVal.Value < 11 && currVal.Value > 9)
            {
                score += 0.15;
            }

            // Voltage rises when engine starts
            if (currVal > prevVal && currVal.Value > 13)
            {
                score += 0.1;
            }

            // Values should be in realistic range
            if (currVal.Value >= 10 && currVal.Value <= 30)
            {
                score += 0.05;
            }

            // Check for common voltages
            var commonVoltages = new[] { 12.0, 12.6, 13.2, 13.8, 14.2, 14.4, 24.0, 26.0, 28.0 };
            foreach (var v in commonVoltages)
            {
                if (Math.Abs(currVal.Value - v) < 0.3)
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

            // Should not exceed 32V (extreme cases)
            if (currVal.Value > 32)
            {
                score -= 0.4;
            }
        }

        // Bonus for normal range
        if (hasNormalRange)
            score += 0.1;

        // Bonus for charging voltage
        if (hasChargingVoltage)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}