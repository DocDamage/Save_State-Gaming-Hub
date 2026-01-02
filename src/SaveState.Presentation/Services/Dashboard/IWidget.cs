using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.Services.Dashboard;

/// <summary>
/// Represents the size of a dashboard widget.
/// </summary>
public enum WidgetSize
{
    /// <summary>
    /// Small widget (3 columns, 1 row).
    /// </summary>
    Small,

    /// <summary>
    /// Medium widget (4 columns, 2 rows).
    /// </summary>
    Medium,

    /// <summary>
    /// Large widget (6 columns, 2 rows).
    /// </summary>
    Large,

    /// <summary>
    /// Wide widget (8 columns, 1 row).
    /// </summary>
    Wide,

    /// <summary>
    /// Tall widget (4 columns, 3 rows).
    /// </summary>
    Tall,

    /// <summary>
    /// Full width widget (12 columns, 2 rows).
    /// </summary>
    Full
}

/// <summary>
/// Interface for dashboard widgets.
/// </summary>
public interface IWidget : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this widget.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display title of the widget.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the icon for the widget.
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Gets the default size of the widget.
    /// </summary>
    WidgetSize DefaultSize { get; }

    /// <summary>
    /// Gets the sizes supported by this widget.
    /// </summary>
    WidgetSize[] SupportedSizes { get; }

    /// <summary>
    /// Gets the refresh interval in milliseconds.
    /// </summary>
    int RefreshIntervalMs { get; }

    /// <summary>
    /// Gets whether this widget can be minimized.
    /// </summary>
    bool CanMinimize { get; }

    /// <summary>
    /// Gets whether this widget can be removed from the dashboard.
    /// </summary>
    bool CanRemove { get; }

    /// <summary>
    /// Gets the view model for this widget.
    /// </summary>
    ObservableObject ViewModel { get; }

    /// <summary>
    /// Initializes the widget asynchronously.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Refreshes the widget data asynchronously.
    /// </summary>
    Task RefreshAsync();
}

/// <summary>
/// Extension methods for widget size calculations.
/// </summary>
public static class WidgetSizeExtensions
{
    /// <summary>
    /// Gets the number of columns for a widget size.
    /// </summary>
    public static int GetColumns(this WidgetSize size) => size switch
    {
        WidgetSize.Small => 3,
        WidgetSize.Medium => 4,
        WidgetSize.Large => 6,
        WidgetSize.Wide => 8,
        WidgetSize.Tall => 4,
        WidgetSize.Full => 12,
        _ => 4
    };

    /// <summary>
    /// Gets the number of rows for a widget size.
    /// </summary>
    public static int GetRows(this WidgetSize size) => size switch
    {
        WidgetSize.Small => 1,
        WidgetSize.Medium => 2,
        WidgetSize.Large => 2,
        WidgetSize.Wide => 1,
        WidgetSize.Tall => 3,
        WidgetSize.Full => 2,
        _ => 2
    };
}