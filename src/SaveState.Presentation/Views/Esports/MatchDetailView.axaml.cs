using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Esports;

namespace SaveState.Presentation.Views.Esports;

/// <summary>
/// View for displaying match details and result reporting.
/// </summary>
public partial class MatchDetailView : UserControl
{
    public MatchDetailView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
