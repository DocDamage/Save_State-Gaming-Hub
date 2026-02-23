using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.CloudGaming;

/// <summary>
/// Code-behind for the Stream Launcher view.
/// </summary>
public partial class StreamLauncherView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the StreamLauncherView.
    /// </summary>
    public StreamLauncherView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
