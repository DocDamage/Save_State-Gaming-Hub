using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Replay;

/// <summary>
/// View for the Replay Theater feature.
/// </summary>
public partial class ReplayTheaterView : UserControl
{
    public ReplayTheaterView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
