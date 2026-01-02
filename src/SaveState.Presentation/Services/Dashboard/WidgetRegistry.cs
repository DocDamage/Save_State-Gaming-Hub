using Microsoft.Extensions.DependencyInjection;
using SaveState.Presentation.Services.Dashboard.Widgets;

namespace SaveState.Presentation.Services.Dashboard;

/// <summary>
/// Registry for managing dashboard widgets.
/// </summary>
public static class WidgetRegistry
{
    /// <summary>
    /// Gets all available widget types.
    /// </summary>
    public static readonly Type[] AvailableWidgets = new[]
    {
        typeof(QuickActionsWidget),
        typeof(TodaysStatsWidget),
        typeof(ActivityFeedWidget),
        typeof(RecentlyAddedWidget),
        typeof(GoalsProgressWidget),
        typeof(EmulatorStatusWidget),
    };

    /// <summary>
    /// Gets the default widget layout for new users.
    /// </summary>
    public static readonly WidgetLayout DefaultLayout = new WidgetLayout
    {
        Widgets = new[]
        {
            new WidgetPosition("quick-actions", 0, 0, WidgetSize.Medium),
            new WidgetPosition("todays-stats", 4, 0, WidgetSize.Medium),
            new WidgetPosition("activity-feed", 0, 2, WidgetSize.Full),
            new WidgetPosition("recently-added", 0, 4, WidgetSize.Medium),
            new WidgetPosition("emulator-status", 4, 4, WidgetSize.Medium),
            new WidgetPosition("goals-progress", 8, 4, WidgetSize.Medium),
        }
    };

    /// <summary>
    /// Creates a widget instance from the service provider.
    /// </summary>
    /// <param name="widgetType">The widget type to create.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>The created widget instance.</returns>
    public static IWidget CreateWidget(Type widgetType, IServiceProvider serviceProvider)
    {
        return (IWidget)serviceProvider.GetRequiredService(widgetType);
    }
}

/// <summary>
/// Represents the layout of widgets on the dashboard.
/// </summary>
public class WidgetLayout
{
    /// <summary>
    /// Gets or sets the widget positions.
    /// </summary>
    public WidgetPosition[] Widgets { get; set; } = Array.Empty<WidgetPosition>();
}

/// <summary>
/// Represents the position and size of a widget.
/// </summary>
public record WidgetPosition(string WidgetId, int Column, int Row, WidgetSize Size);
