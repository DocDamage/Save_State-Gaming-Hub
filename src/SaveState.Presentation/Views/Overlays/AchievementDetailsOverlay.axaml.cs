using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SaveState.Presentation.Views.Overlays;

/// <summary>
/// Achievement details overlay showing unlock progress, rarity, and tips.
/// </summary>
public partial class AchievementDetailsOverlay : UserControl
{
    public AchievementDetailsOverlay()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.Overlays.AchievementDetailsOverlayViewModel viewModel)
        {
            viewModel.CloseCommand.Execute(null);
        }
    }
}
