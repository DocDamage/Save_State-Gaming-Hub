using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Widget showing today's gaming statistics.
/// </summary>
public partial class TodaysStatsWidget : WidgetBase
{
    private readonly IMediator _mediator;

    public TodaysStatsWidget(IMediator mediator, ILogger<TodaysStatsWidget> logger)
        : base(logger)
    {
        _mediator = mediator;
    }

    /// <inheritdoc />
    public override string Id => "todays-stats";

    /// <inheritdoc />
    public override string Title => "Today's Stats";

    /// <inheritdoc />
    public override string Icon => "📈";

    /// <inheritdoc />
    public override WidgetSize DefaultSize => WidgetSize.Medium;

    /// <inheritdoc />
    public override WidgetSize[] SupportedSizes => new[] { WidgetSize.Medium, WidgetSize.Large };

    /// <inheritdoc />
    public override int RefreshIntervalMs => 60000; // Refresh every minute

    /// <summary>
    /// Gets the playtime for today.
    /// </summary>
    [ObservableProperty]
    private TimeSpan _playtime;

    /// <summary>
    /// Gets the formatted playtime string.
    /// </summary>
    public string PlaytimeText => Playtime.TotalHours >= 1
        ? $"{Playtime.TotalHours:F1}h"
        : $"{Playtime.TotalMinutes:F0}m";

    /// <summary>
    /// Gets the number of sessions today.
    /// </summary>
    [ObservableProperty]
    private int _sessions;

    /// <summary>
    /// Gets the number of achievements unlocked today.
    /// </summary>
    [ObservableProperty]
    private int _achievements;

    /// <summary>
    /// Gets the formatted sessions string.
    /// </summary>
    public string SessionsText => $"{Sessions} sessions";

    /// <summary>
    /// Gets the formatted achievements string.
    /// </summary>
    public string AchievementsText => $"{Achievements} achievements";

    /// <inheritdoc />
    protected override async Task LoadDataAsync()
    {
        // TODO: Get today's stats from analytics service
        // For now, simulate some data
        Playtime = TimeSpan.FromHours(2.5);
        Sessions = 3;
        Achievements = 5;

        // In real implementation:
        // var stats = await _mediator.Send(new GetTodaysStatsQuery());
        // Playtime = stats.Playtime;
        // Sessions = stats.Sessions;
        // Achievements = stats.Achievements;
    }
}