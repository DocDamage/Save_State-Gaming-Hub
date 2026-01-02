using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Shell;

/// <summary>
/// Container for all application overlays.
/// </summary>
public partial class OverlayContainer : UserControl
{
    public OverlayContainer()
    {
        InitializeComponent();
    }

    private void OnDimPressed(object? sender, PointerPressedEventArgs e)
    {
        // Close modal overlays when clicking on dimmed background
        var viewModel = DataContext as ViewModels.Shell.OverlayContainerViewModel;
        viewModel?.CloseModalOverlays();
    }

    private void OnPerformanceHudPressed(object? sender, PointerPressedEventArgs e)
    {
        // Allow dragging the performance HUD
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var window = (Window?)this.VisualRoot;
            window?.BeginMoveDrag(e);
        }
    }
}