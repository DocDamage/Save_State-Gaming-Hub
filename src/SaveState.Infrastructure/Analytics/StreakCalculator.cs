using System;
using System.Collections.Generic;
using System.Linq;
using SaveState.Core.Analytics.DTOs;
using SaveState.Core.Analytics.Services;

namespace SaveState.Infrastructure.Analytics;

/// <summary>
/// Shared helper that builds streak metadata from daily activities.
/// </summary>
public sealed class StreakCalculator : IStreakCalculator
{
    /// <inheritdoc />
    public StreakCalculationResult Calculate(
        IReadOnlyDictionary<DateOnly, DailyActivity> activities,
        DateOnly referenceDate)
    {
        var streaks = new List<StreakExportData>();
        DateOnly? streakStart = null;
        DateOnly? previousDate = null;
        int streakDays = 0;
        int streakSessions = 0;

        foreach (var date in activities.Keys.OrderBy(d => d))
        {
            var activity = activities[date];

            if (!IsActiveDay(activity.Level))
            {
                FlushStreak();
                previousDate = null;
                continue;
            }

            if (streakStart == null ||
                (previousDate.HasValue && date.DayNumber - previousDate.Value.DayNumber != 1))
            {
                FlushStreak();
                streakStart = date;
                streakDays = 1;
                streakSessions = activity.SessionCount;
            }
            else
            {
                streakDays++;
                streakSessions += activity.SessionCount;
            }

            previousDate = date;
        }

        FlushStreak();

        var longest = streaks.Count > 0 ? streaks.Max(s => s.DaysCount) : 0;
        var current = CalculateCurrentStreak(activities, referenceDate);

        return new StreakCalculationResult(
            CurrentStreak: current,
            LongestStreak: longest,
            Streaks: streaks.AsReadOnly());

        void FlushStreak()
        {
            if (streakStart == null || previousDate == null || streakDays <= 0)
            {
                return;
            }

            streaks.Add(new StreakExportData(
                StartDate: streakStart.Value.ToDateTime(TimeOnly.MinValue),
                EndDate: previousDate.Value.ToDateTime(TimeOnly.MinValue),
                DaysCount: streakDays,
                TotalSessions: streakSessions));
            streakStart = null;
            streakDays = 0;
            streakSessions = 0;
        }
    }

    private static int CalculateCurrentStreak(
        IReadOnlyDictionary<DateOnly, DailyActivity> activities,
        DateOnly referenceDate)
    {
        var streak = 0;
        var currentDate = referenceDate;

        while (activities.TryGetValue(currentDate, out var activity) && IsActiveDay(activity.Level))
        {
            streak++;
            currentDate = currentDate.AddDays(-1);
        }

        return streak;
    }

    private static bool IsActiveDay(ActivityLevel level) =>
        level != ActivityLevel.None;
}
