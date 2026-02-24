using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using SaveState.Presentation.Services.Dashboard;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Dashboard tab.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly IOverlayService _overlayService;
    private readonly IServiceProvider _serviceProvider;

    public DashboardViewModel(
        ILogger<DashboardViewModel> logger,
        IOverlayService overlayService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _overlayService = overlayService;
        _serviceProvider = serviceProvider;

        Widgets = new ObservableCollection<WidgetInstance>();
        _ = InitializeWidgetsAsync();
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
    /// Initializes the default set of widgets asynchronously.
    /// </summary>
    /// <summary>
    /// Initializes the default set of widgets asynchronously.
    /// </summary>
    private async Task InitializeWidgetsAsync()
    {
        try
        {
            var layout = WidgetRegistry.DefaultLayout;
            _logger.LogInformation("Initializing dashboard with {Count} widgets", layout.Widgets.Length);

            foreach (var position in layout.Widgets)
            {
                var widgetType = WidgetRegistry.AvailableWidgets.FirstOrDefault(t =>
                    t.Name.Replace("Widget", "", StringComparison.OrdinalIgnoreCase)
                         .Equals(position.WidgetId.Replace("-", ""), StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Contains(position.WidgetId.Replace("-", ""), StringComparison.OrdinalIgnoreCase));

                if (widgetType == null)
                {
                    _logger.LogWarning("Could not find widget type for ID: {WidgetId}", position.WidgetId);
                    continue;
                }

                try
                {
                    var widget = WidgetRegistry.CreateWidget(widgetType, _serviceProvider);
                    await widget.InitializeAsync();

                    var instance = new WidgetInstance(widget, position.Column, position.Row);

                    // Safe update on UI thread
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Widgets.Add(instance);
                    });
                }
                catch (Exception wEx)
                {
                    _logger.LogError(wEx, "Failed to create/init widget {WidgetId}", position.WidgetId);
                }
            }

            _logger.LogInformation("Dashboard widgets initialized successfully");
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
        _logger.LogInformation("Dashboard customization requested");
        _overlayService.ShowDashboardCustomizationDialog();
    }

    /// <summary>
    /// Command to remove a widget from the dashboard.
    /// </summary>
    [RelayCommand]
    private void RemoveWidget(WidgetInstance widgetInstance)
    {
        if (widgetInstance == null) return;

        Widgets.Remove(widgetInstance);
        _logger.LogInformation("Removed widget instance {WidgetId}", widgetInstance.Widget.Id);
    }

    /// <summary>
    /// Command to reset the dashboard layout.
    /// </summary>
    [RelayCommand]
    private async Task ResetLayout()
    {
        _logger.LogInformation("Resetting dashboard layout to default");
        Widgets.Clear();
        await InitializeWidgetsAsync();
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
