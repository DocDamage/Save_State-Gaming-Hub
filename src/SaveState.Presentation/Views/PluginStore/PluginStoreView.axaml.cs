using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.PluginStore;

/// <summary>
/// Code-behind for the Plugin Store view.
/// </summary>
public partial class PluginStoreView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the PluginStoreView class.
    /// </summary>
    public PluginStoreView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
