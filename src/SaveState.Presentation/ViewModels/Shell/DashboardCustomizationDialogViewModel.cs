using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class DashboardCustomizationDialogViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<DashboardWidgetViewModel> _availableWidgets = new();

    [ObservableProperty]
    private ObservableCollection<DashboardWidgetViewModel> _activeWidgets = new();

    public DashboardCustomizationDialogViewModel(
        IOverlayService overlayService,
        INotificationService notificationService)
    {
        _overlayService = overlayService;
        _notificationService = notificationService;
        LoadWidgets();
    }

    private void LoadWidgets()
    {
        // Available widgets
        AvailableWidgets.Clear();
        AvailableWidgets.Add(new DashboardWidgetViewModel("Recent Games", "🎮", false));
        AvailableWidgets.Add(new DashboardWidgetViewModel("Performance Stats", "📊", false));
        AvailableWidgets.Add(new DashboardWidgetViewModel("Achievements", "🏆", false));
        AvailableWidgets.Add(new DashboardWidgetViewModel("Quick Actions", "⚡", false));
        AvailableWidgets.Add(new DashboardWidgetViewModel("System Monitor", "💻", false));
        AvailableWidgets.Add(new DashboardWidgetViewModel("Friend Activity", "👥", false));

        // Active widgets (currently on dashboard)
        ActiveWidgets.Clear();
        ActiveWidgets.Add(new DashboardWidgetViewModel("Recent Games", "🎮", true));
        ActiveWidgets.Add(new DashboardWidgetViewModel("Performance Stats", "📊", true));
        ActiveWidgets.Add(new DashboardWidgetViewModel("Quick Actions", "⚡", true));
    }

    [RelayCommand]
    private void AddWidget(DashboardWidgetViewModel widget)
    {
        if (!ActiveWidgets.Any(w => w.Name == widget.Name))
        {
            var activeWidget = new DashboardWidgetViewModel(widget.Name, widget.Icon, true);
            ActiveWidgets.Add(activeWidget);
            _notificationService.ShowSuccess($"Added {widget.Name} to dashboard", "Customization");
        }
    }

    [RelayCommand]
    private void RemoveWidget(DashboardWidgetViewModel widget)
    {
        ActiveWidgets.Remove(widget);
        _notificationService.ShowInfo($"Removed {widget.Name} from dashboard", "Customization");
    }

    [RelayCommand]
    private void MoveUp(DashboardWidgetViewModel widget)
    {
        var index = ActiveWidgets.IndexOf(widget);
        if (index > 0)
        {
            ActiveWidgets.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private void MoveDown(DashboardWidgetViewModel widget)
    {
        var index = ActiveWidgets.IndexOf(widget);
        if (index < ActiveWidgets.Count - 1)
        {
            ActiveWidgets.Move(index, index + 1);
        }
    }

    [RelayCommand]
    private void ResetToDefault()
    {
        LoadWidgets();
        _notificationService.ShowInfo("Dashboard reset to default layout", "Customization");
    }

    [RelayCommand]
    private void Save()
    {
        _notificationService.ShowSuccess("Dashboard layout saved", "Customization");
        Close();
    }

    [RelayCommand]
    private void Close()
    {
        _overlayService.HideDashboardCustomizationDialog();
    }
}

public partial class DashboardWidgetViewModel : ObservableObject
{
    public DashboardWidgetViewModel(string name, string icon, bool isActive)
    {
        Name = name;
        Icon = icon;
        IsActive = isActive;
    }

    public string Name { get; }
    public string Icon { get; }

    [ObservableProperty]
    private bool isActive;
}
