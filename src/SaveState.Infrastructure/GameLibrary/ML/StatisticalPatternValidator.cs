using System.Diagnostics;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.ML;

/// <summary>
/// Validates patterns using statistical analysis.
/// </summary>
public sealed class StatisticalPatternValidator
{
    /// <summary>
    /// Validates a pattern based on value history using statistical methods.
    /// </summary>
    public ValidationResult ValidatePattern(
        long address,
        List<ValueObservation> history,
        string patternType)
    {
        if (history is null || history.Count < 3)
        {
            return new ValidationResult
            {
                IsValid = false,
                Confidence = 0.3,
                Reasoning = "Insufficient data for statistical validation (need at least 3 observations)",
                SuggestedAction = ValidationAction.RequestMoreData
            };
        }

        var values = ConvertToDoubles(history.Select(h => h.Value)).ToList();
        if (values.Count < 3)
        {
            return new ValidationResult
            {
                IsValid = false,
                Confidence = 0.3,
                Reasoning = "Insufficient numeric data for statistical validation",
                SuggestedAction = ValidationAction.RequestMoreData
            };
        }
        var metrics = CalculateMetrics(values);

        // Determine expected behavior based on pattern type
        var expectedBehavior = GetExpectedBehavior(patternType);

        var validationScore = 0.0;
        var reasons = new List<string>();

        // Check standard deviation (stable values have low std dev relative to mean)
        var cv = metrics.Mean != 0 ? metrics.StdDev / Math.Abs(metrics.Mean) : metrics.StdDev;
        if (cv < 0.5)
        {
            validationScore += 0.25;
            reasons.Add($"Low coefficient of variation ({cv:F2}) indicates stable value");
        }
        else if (cv > 2.0 && expectedBehavior.AllowsHighVariance)
        {
            validationScore += 0.15;
            reasons.Add($"High variance acceptable for {patternType}");
        }

        // Check change frequency
        var changeFrequency = CalculateChangeFrequency(values);
        if (changeFrequency >= expectedBehavior.MinChangeFrequency &&
            changeFrequency <= expectedBehavior.MaxChangeFrequency)
        {
            validationScore += 0.25;
            reasons.Add($"Change frequency ({changeFrequency:F2}) matches expected behavior");
        }

        // Check value distribution
        if (IsNormalDistribution(values, metrics))
        {
            validationScore += 0.2;
            reasons.Add("Value distribution appears normal");
        }

        // Check for outliers
        var outlierRatio = CalculateOutlierRatio(values, metrics);
        if (outlierRatio < 0.1)
        {
            validationScore += 0.2;
            reasons.Add($"Low outlier ratio ({outlierRatio:F2}) indicates consistent values");
        }
        else if (outlierRatio > 0.3)
        {
            validationScore -= 0.2;
            reasons.Add($"High outlier ratio ({outlierRatio:F2}) suggests possible false positive");
        }

        // Check range validity
        if (values.All(v => v >= expectedBehavior.MinValue && v <= expectedBehavior.MaxValue))
        {
            validationScore += 0.1;
            reasons.Add($"Values within expected range for {patternType}");
        }

        var isValid = validationScore >= 0.5;
        var confidence = Math.Min(Math.Max(validationScore, 0.1), 0.95);

        return new ValidationResult
        {
            IsValid = isValid,
            Confidence = confidence,
            Reasoning = string.Join("; ", reasons),
            Metrics = new Dictionary<string, double>
            {
                ["Mean"] = metrics.Mean,
                ["StdDev"] = metrics.StdDev,
                ["CoefficientOfVariation"] = cv,
                ["ChangeFrequency"] = changeFrequency,
                ["OutlierRatio"] = outlierRatio,
                ["Min"] = metrics.Min,
                ["Max"] = metrics.Max,
                ["Range"] = metrics.Max - metrics.Min
            },
            SuggestedAction = isValid ? ValidationAction.Accept :
                             confidence < 0.3 ? ValidationAction.RequestMoreData :
                             ValidationAction.ManualReview
        };
    }

    private StatisticalMetrics CalculateMetrics(List<double> values)
    {
        var mean = values.Average();
        var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        return new StatisticalMetrics
        {
            Mean = mean,
            StdDev = stdDev,
            Variance = variance,
            Min = values.Min(),
            Max = values.Max()
        };
    }

    private double CalculateChangeFrequency(List<double> values)
    {
        if (values.Count < 2) return 0;

        var changes = 0;
        for (int i = 1; i < values.Count; i++)
        {
            if (Math.Abs(values[i] - values[i - 1]) > 0.001)
                changes++;
        }

        return (double)changes / (values.Count - 1);
    }

    private bool IsNormalDistribution(List<double> values, StatisticalMetrics metrics)
    {
        // Simple check using skewness and kurtosis approximation
        if (values.Count < 4) return false;

        var skewness = CalculateSkewness(values, metrics);
        return Math.Abs(skewness) < 1.0; // Rough approximation
    }

    private double CalculateSkewness(List<double> values, StatisticalMetrics metrics)
    {
        if (metrics.StdDev == 0) return 0;

        var n = values.Count;
        var sumCubedDeviations = values.Sum(v => Math.Pow((v - metrics.Mean) / metrics.StdDev, 3));
        return sumCubedDeviations * n / ((n - 1) * (n - 2));
    }

    private double CalculateOutlierRatio(List<double> values, StatisticalMetrics metrics)
    {
        if (metrics.StdDev == 0) return 0;

        var threshold = 2.0 * metrics.StdDev; // 2-sigma rule
        var outliers = values.Count(v => Math.Abs(v - metrics.Mean) > threshold);
        return (double)outliers / values.Count;
    }

    private static IEnumerable<double> ConvertToDoubles(IEnumerable<object?> values)
    {
        foreach (var value in values)
        {
            if (value is null) continue;

            if (value is double d) yield return d;
            else if (value is float f) yield return f;
            else if (value is int i) yield return i;
            else if (value is long l) yield return l;
            else if (value is decimal dec) yield return (double)dec;
            else if (double.TryParse(value.ToString(), out var parsed))
                yield return parsed;
        }
    }

    private ExpectedBehavior GetExpectedBehavior(string patternType)
    {
        return patternType.ToLowerInvariant() switch
        {
            "health" => new ExpectedBehavior
            {
                MinValue = 0,
                MaxValue = 100000,
                MinChangeFrequency = 0.05,
                MaxChangeFrequency = 0.5,
                AllowsHighVariance = true
            },
            "ammo" => new ExpectedBehavior
            {
                MinValue = 0,
                MaxValue = 9999,
                MinChangeFrequency = 0.1,
                MaxChangeFrequency = 0.8,
                AllowsHighVariance = true
            },
            "currency" => new ExpectedBehavior
            {
                MinValue = 0,
                MaxValue = 999999999,
                MinChangeFrequency = 0.01,
                MaxChangeFrequency = 0.3,
                AllowsHighVariance = false
            },
            "experience" => new ExpectedBehavior
            {
                MinValue = 0,
                MaxValue = 999999999,
                MinChangeFrequency = 0.01,
                MaxChangeFrequency = 0.2,
                AllowsHighVariance = false
            },
            "level" => new ExpectedBehavior
            {
                MinValue = 1,
                MaxValue = 9999,
                MinChangeFrequency = 0,
                MaxChangeFrequency = 0.05,
                AllowsHighVariance = false
            },
            "timer" => new ExpectedBehavior
            {
                MinValue = 0,
                MaxValue = 86400,
                MinChangeFrequency = 0.8,
                MaxChangeFrequency = 1.0,
                AllowsHighVariance = false
            },
            "position" => new ExpectedBehavior
            {
                MinValue = -999999,
                MaxValue = 999999,
                MinChangeFrequency = 0.5,
                MaxChangeFrequency = 1.0,
                AllowsHighVariance = true
            },
            _ => new ExpectedBehavior
            {
                MinValue = double.MinValue,
                MaxValue = double.MaxValue,
                MinChangeFrequency = 0,
                MaxChangeFrequency = 1.0,
                AllowsHighVariance = true
            }
        };
    }

    private sealed class StatisticalMetrics
    {
        public double Mean { get; set; }
        public double StdDev { get; set; }
        public double Variance { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
    }

    private sealed class ExpectedBehavior
    {
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double MinChangeFrequency { get; set; }
        public double MaxChangeFrequency { get; set; }
        public bool AllowsHighVariance { get; set; }
    }
}
