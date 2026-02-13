using System.Collections.Generic;
using SaveState.Core.Analytics.DTOs;

namespace SaveState.Core.Analytics.Services;

/// <summary>
/// Calculates play streak metrics based on daily activity.
/// </summary>
public interface IStreakCalculator
{
    /// <summary>
    /// Calculates streak statistics for the provided activity history.
    /// </summary>
    /// <param name="activities">Daily activity map keyed by date.</param>
    /// <param name="referenceDate">Date to treat as today when computing the current streak.</param>
    /// <returns>Summary of the current streak, longest streak, and list of streak segments.</returns>
    StreakCalculationResult Calculate(
        IReadOnlyDictionary<DateOnly, DailyActivity> activities,
        DateOnly referenceDate);
}

/// <summary>
/// Result returned by the streak calculator.
/// </summary>
public sealed record StreakCalculationResult(
    int CurrentStreak,
    int LongestStreak,
    IReadOnlyList<StreakExportData> Streaks);
