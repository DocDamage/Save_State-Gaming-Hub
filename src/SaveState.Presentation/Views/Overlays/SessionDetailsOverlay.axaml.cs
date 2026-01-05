using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SaveState.Presentation.Views.Overlays;

/// <summary>
/// Session details overlay showing playtime charts and performance graphs.
/// </summary>
public partial class SessionDetailsOverlay : UserControl
{
    public SessionDetailsOverlay()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        IsVisible = false;
    }
}
