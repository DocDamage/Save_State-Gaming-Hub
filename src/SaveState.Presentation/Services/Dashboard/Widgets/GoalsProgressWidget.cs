using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SaveState.Core.Analytics;
using SaveState.Core.Analytics.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Widget showing current goal progress.
/// </summary>
public partial class GoalsProgressWidget : WidgetBase
{
    private readonly IGamingGoalRepository _goalRepository;
    private readonly IGoalService _goalService;

    public GoalsProgressWidget(
        IGamingGoalRepository goalRepository,
        IGoalService goalService,
        ILogger<GoalsProgressWidget> logger)
        : base(logger)
    {
        _goalRepository = goalRepository;
        _goalService = goalService;
        Goals = new ObservableCollection<GoalItem>();
    }

    /// <inheritdoc />
    public override string Id => "goals-progress";

    /// <inheritdoc />
    public override string Title => "Goal Progress";

    /// <inheritdoc />
    public override string Icon => "🎯";

    /// <inheritdoc />
    public override WidgetSize DefaultSize => WidgetSize.Medium;

    /// <inheritdoc />
    public override WidgetSize[] SupportedSizes => new[] { WidgetSize.Medium, WidgetSize.Small };

    /// <inheritdoc />
    public override int RefreshIntervalMs => 60000; // 1 minute

    /// <summary>
    /// Gets the collection of current goals.
    /// </summary>
    public ObservableCollection<GoalItem> Goals { get; }

    /// <inheritdoc />
    protected override async Task LoadDataAsync()
    {
        try
        {
            Goals.Clear();

            // Get active goals from the repository
            var activeGoals = await _goalRepository.GetActiveGoalsAsync();

            foreach (var goal in activeGoals.OrderBy(g => g.EndDate).Take(5))
            {
                var goalType = DetermineGoalType(goal);
                var current = goal.CurrentValue;
                var target = goal.TargetValue;

                Goals.Add(new GoalItem(
                    goal.Title,
                    (int)current,
                    (int)target,
                    goalType));
            }

            // If no goals exist, show a prompt
            if (Goals.Count == 0)
            {
                Goals.Add(new GoalItem(
                    "No active goals - create one to start tracking!",
                    0,
                    1,
                    GoalType.Custom));
            }

            Logger.LogInformation("Loaded {Count} active goals", Goals.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load goals data");

            // Fallback data
            Goals.Clear();
            Goals.Add(new GoalItem("Error loading goals", 0, 1, GoalType.Custom));
        }
    }

    private static GoalType DetermineGoalType(SaveState.Core.Analytics.Entities.GamingGoal goal)
    {
        if (goal.Title.Contains("complete", StringComparison.OrdinalIgnoreCase) ||
            goal.Title.Contains("finish", StringComparison.OrdinalIgnoreCase))
            return GoalType.Completion;

        if (goal.Title.Contains("hour", StringComparison.OrdinalIgnoreCase) ||
            goal.Title.Contains("playtime", StringComparison.OrdinalIgnoreCase))
            return GoalType.Playtime;

        if (goal.Title.Contains("achievement", StringComparison.OrdinalIgnoreCase))
            return GoalType.Achievements;

        return GoalType.Custom;
    }
}

/// <summary>
/// Represents a goal item.
/// </summary>
public record GoalItem(string Title, int Current, int Target, GoalType Type)
{
    /// <summary>
    /// Gets the progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage => Target > 0 ? (double)Current / Target * 100 : 0;

    /// <summary>
    /// Gets the formatted progress text.
    /// </summary>
    public string ProgressText => $"{Current}/{Target}";
}

/// <summary>
/// Goal type enumeration.
/// </summary>
public enum GoalType
{
    Completion,
    Playtime,
    Achievements,
    Custom
}
