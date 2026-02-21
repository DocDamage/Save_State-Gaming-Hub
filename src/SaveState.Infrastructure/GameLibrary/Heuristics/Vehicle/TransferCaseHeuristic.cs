using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting transfer case position/state in driving/racing games.
/// Transfer case values typically:
/// - Are integers representing modes (2H=0, 4H=1, 4L=2, N=3)
/// - Change based on terrain and driving conditions
/// - Found in 4x4/off-road vehicles
/// - Affect torque distribution
/// </summary>
public sealed class TransferCaseHeuristic : IValueHeuristic
{
    public string Name => "Transfer Case Detection";
    public string Category => "Vehicle";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        bool hasModeChange = false;
        var validModes = new HashSet<int> { 0, 1, 2, 3 };

        // Check value range (0-3 for standard modes)
        var currentVal = HeuristicUtilities.ConvertToDouble(value.CurrentValue);
        if (currentVal.HasValue)
        {
            int intVal = (int)currentVal.Value;
            if (validModes.Contains(intVal))
            {
                score += 0.45;
            }
            else if (currentVal.Value >= 0 && currentVal.Value <= 10)
            {
                score += 0.2;
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

            int prevInt = (int)prevVal.Value;
            int currInt = (int)currVal.Value;

            // Check for mode changes
            if (prevInt != currInt)
            {
                hasModeChange = true;
                // Valid mode transitions get higher score
                if (validModes.Contains(currInt) && validModes.Contains(prevInt))
                {
                    score += 0.15;
                }
            }

            // Values should be small integers
            if (currInt >= 0 && currInt <= 10)
            {
                score += 0.05;
            }

            // Should not be negative
            if (currVal.Value < 0)
            {
                score -= 0.4;
            }

            // Should not exceed reasonable mode count
            if (currVal.Value > 20)
            {
                score -= 0.4;
            }
        }

        // Bonus for mode changes (characteristic of transfer case)
        if (hasModeChange && history.Count > 2)
            score += 0.2;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType) => valueType.ToLowerInvariant() is "float" or "single" or "double" or "int32" or "int";
}