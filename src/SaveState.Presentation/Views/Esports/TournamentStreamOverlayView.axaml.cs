using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Esports;

namespace SaveState.Presentation.Views.Esports;

/// <summary>
/// Compact overlay view for tournament streaming.
/// </summary>
public partial class TournamentStreamOverlayView : UserControl
{
    public TournamentStreamOverlayView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
