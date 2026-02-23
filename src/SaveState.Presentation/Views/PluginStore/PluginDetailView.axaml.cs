using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.PluginStore;

/// <summary>
/// Code-behind for the Plugin Detail view.
/// </summary>
public partial class PluginDetailView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the PluginDetailView class.
    /// </summary>
    public PluginDetailView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
