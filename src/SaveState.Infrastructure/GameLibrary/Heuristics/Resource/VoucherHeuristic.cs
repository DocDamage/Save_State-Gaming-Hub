using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting voucher/redemption code count in mobile/F2P games.
/// Voucher values typically:
/// - Are integers (0-20)
/// - Increase from special events, promotions, or codes
/// - Decrease when redeeming for premium items
/// </summary>
public sealed class VoucherHeuristic : IValueHeuristic
{
    public string Name => "Vouchers/Redemption Items Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int redeemEvents = 0;

        // Check value range (vouchers typically 0-20, extremely limited)
        if (IsInVoucherRange(value.CurrentValue))
        {
            score += 0.5;
        }

        // Must be integer
        if (!HeuristicUtilities.IsIntegerValue(value.CurrentValue))
        {
            score -= 0.2;
        }
        else
        {
            score += 0.1;
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

            // Check for gain (events/codes)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Vouchers gained 1-3 at a time
                if (delta >= 1 && delta <= 5)
                {
                    score += 0.2;
                }
            }

            // Check for redeem (using voucher)
            if (currVal < prevVal)
            {
                redeemEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Redeeming uses exactly 1 voucher
                if (delta == 1)
                {
                    score += 0.3;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Very strong bonus for single-use pattern with extremely low max
        if (redeemEvents >= 1)
            score += 0.2;
        if (gainEvents >= 1)
            score += 0.1;

        return Math.Clamp(score, 0.0, 1.0);
    }

    public bool SupportsValueType(string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();
        return normalizedType is "int32" or "int" or "int16" or "short";
    }

    private static bool IsInVoucherRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 100;
        }
        catch
        {
            return false;
        }
    }
}