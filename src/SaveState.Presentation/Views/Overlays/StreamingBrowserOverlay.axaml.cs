using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Overlays;

/// <summary>
/// Streaming browser overlay window for in-game web browsing.
/// </summary>
public partial class StreamingBrowserOverlay : Window
{
    public StreamingBrowserOverlay()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
