using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SaveState.Presentation.Views.Overlays;

/// <summary>
/// Mod details overlay showing mod info, changelog, and conflicts.
/// </summary>
public partial class ModDetailsOverlay : UserControl
{
    public ModDetailsOverlay()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.Overlays.ModDetailsOverlayViewModel viewModel)
        {
            viewModel.CloseCommand.Execute(null);
        }
    }
}
