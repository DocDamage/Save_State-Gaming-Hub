using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services.Dashboard;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Dashboard tab.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DashboardViewModel(
        ILogger<DashboardViewModel> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        Widgets = new ObservableCollection<WidgetInstance>();
        InitializeWidgets();
    }

    /// <summary>
    /// Gets the collection of widgets on the dashboard.
    /// </summary>
    public ObservableCollection<WidgetInstance> Widgets { get; }

    /// <summary>
    /// Gets the display title for the dashboard.
    /// </summary>
    public string Title => "Dashboard";

    /// <summary>
    /// Initializes the default set of widgets.
    /// </summary>
    private async void InitializeWidgets()
    {
        try
        {
            var layout = WidgetRegistry.DefaultLayout;
            foreach (var position in layout.Widgets)
            {
                var widgetType = WidgetRegistry.AvailableWidgets.FirstOrDefault(t =>
                    t.Name.Contains(position.WidgetId.Replace("-", ""), StringComparison.OrdinalIgnoreCase) ||
                    t.Name.EndsWith(position.WidgetId.Replace("-", "") + "Widget", StringComparison.OrdinalIgnoreCase));

                if (widgetType == null)
                {
                    // Fallback search if name mapping fails
                    _logger.LogWarning("Could not find widget type for ID: {WidgetId}", position.WidgetId);
                    continue;
                }

                var widget = WidgetRegistry.CreateWidget(widgetType, _serviceProvider);
                await widget.InitializeAsync();

                var instance = new WidgetInstance(widget, position.Column, position.Row);
                Widgets.Add(instance);
            }

            _logger.LogInformation("Dashboard widgets initialized with default layout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize dashboard widgets");
        }
    }

    /// <summary>
    /// Command to customize the dashboard layout.
    /// </summary>
    [RelayCommand]
    private void Customize()
    {
        // TODO: Open customization dialog
        _logger.LogInformation("Customize dashboard requested");
    }

    /// <summary>
    /// Command to reset the dashboard layout.
    /// </summary>
    [RelayCommand]
    private void ResetLayout()
    {
        // TODO: Reset to default layout
        _logger.LogInformation("Reset dashboard layout requested");
    }
}

/// <summary>
/// Represents an instance of a widget with its position.
/// </summary>
public class WidgetInstance : ObservableObject
{
    private int _column;
    private int _row;

    public WidgetInstance(IWidget widget, int column, int row)
    {
        Widget = widget;
        Column = column;
        Row = row;
    }

    /// <summary>
    /// Gets the widget.
    /// </summary>
    public IWidget Widget { get; }

    /// <summary>
    /// Gets or sets the column position.
    /// </summary>
    public int Column
    {
        get => _column;
        set => SetProperty(ref _column, value);
    }

    /// <summary>
    /// Gets or sets the row position.
    /// </summary>
    public int Row
    {
        get => _row;
        set => SetProperty(ref _row, value);
    }

    /// <summary>
    /// Gets the column span based on widget size.
    /// </summary>
    public int ColumnSpan => Widget.DefaultSize.GetColumns();

    /// <summary>
    /// Gets the row span based on widget size.
    /// </summary>
    public int RowSpan => Widget.DefaultSize.GetRows();
}
