using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Esports;

namespace SaveState.Presentation.Views.Esports;

/// <summary>
/// View for live tournament tracking with real-time updates.
/// </summary>
public partial class LiveTournamentTrackerView : UserControl
{
    public LiveTournamentTrackerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
