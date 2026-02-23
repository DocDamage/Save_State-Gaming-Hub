using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Overlays;

/// <summary>
/// In-stream performance overlay for cloud gaming sessions.
/// </summary>
public partial class StreamOverlay : UserControl
{
    /// <summary>
    /// Initializes a new instance of the StreamOverlay.
    /// </summary>
    public StreamOverlay()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
