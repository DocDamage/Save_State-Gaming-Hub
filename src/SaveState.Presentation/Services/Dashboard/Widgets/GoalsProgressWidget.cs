using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Widget showing current goal progress.
/// </summary>
public partial class GoalsProgressWidget : WidgetBase
{
    public GoalsProgressWidget(ILogger<GoalsProgressWidget> logger)
        : base(logger)
    {
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
        Goals.Clear();

        // TODO: Get real goals from goals service
        // For now, simulate some goals
        Goals.Add(new GoalItem("Complete 5 games this month", 3, 5, GoalType.Completion));
        Goals.Add(new GoalItem("Play for 50 hours", 32, 50, GoalType.Playtime));
        Goals.Add(new GoalItem("Unlock 20 achievements", 15, 20, GoalType.Achievements));

        await Task.CompletedTask; // Simulate async operation
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