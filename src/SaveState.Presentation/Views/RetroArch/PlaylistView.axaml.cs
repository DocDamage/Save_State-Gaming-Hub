using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.RetroArch;

/// <summary>
/// Code-behind for the PlaylistView.
/// </summary>
public partial class PlaylistView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistView"/> class.
    /// </summary>
    public PlaylistView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
