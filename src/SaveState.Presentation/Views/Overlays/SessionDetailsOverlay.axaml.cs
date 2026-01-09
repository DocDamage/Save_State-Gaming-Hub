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
        if (DataContext is ViewModels.Overlays.SessionDetailsOverlayViewModel viewModel)
        {
            viewModel.CloseCommand.Execute(null);
        }
    }
}
