using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Esports;

namespace SaveState.Presentation.Views.Esports;

/// <summary>
/// View for displaying tournament standings.
/// </summary>
public partial class TournamentStandingsView : UserControl
{
    public TournamentStandingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
