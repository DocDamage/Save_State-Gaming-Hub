using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Heuristics;

/// <summary>
/// Heuristic for detecting coupon/discount voucher count in simulation/shopping games.
/// Coupon values typically:
/// - Are integers (0-50)
/// - Increase from promotions, rewards, or mail
/// - Decrease when making purchases with discounts
/// </summary>
public sealed class CouponHeuristic : IValueHeuristic
{
    public string Name => "Coupons/Discount Vouchers Detection";
    public string Category => "Resource";

    public double CalculateConfidence(DiscoveredValue value, List<ValueObservation> history)
    {
        if (!SupportsValueType(value.ValueType))
            return 0.0;

        double score = 0.0;
        int gainEvents = 0;
        int useEvents = 0;

        // Check value range (coupons typically 0-50, very limited)
        if (IsInCouponRange(value.CurrentValue))
        {
            score += 0.45;
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

            // Check for gain (rewards/promotions)
            if (currVal > prevVal)
            {
                gainEvents++;
                var delta = currVal.Value - prevVal.Value;
                // Coupons gained 1-5 at a time
                if (delta >= 1 && delta <= 10)
                {
                    score += 0.18;
                }
            }

            // Check for use (redeeming)
            if (currVal < prevVal)
            {
                useEvents++;
                var delta = prevVal.Value - currVal.Value;
                // Using coupon decreases by exactly 1
                if (delta == 1)
                {
                    score += 0.25;
                }
            }

            // Should not be negative
            if (currVal < 0)
            {
                score -= 0.5;
            }
        }

        // Bonus for single-use pattern
        if (useEvents >= 1)
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

    private static bool IsInCouponRange(object? value)
    {
        if (value == null) return false;

        try
        {
            var doubleValue = HeuristicUtilities.ConvertToDouble(value);
            if (!doubleValue.HasValue) return false;

            var val = doubleValue.Value;
            return val >= 0 && val <= 500;
        }
        catch
        {
            return false;
        }
    }
}