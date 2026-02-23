using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.PluginStore;

/// <summary>
/// Code-behind for the Plugin Review view.
/// </summary>
public partial class PluginReviewView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the PluginReviewView class.
    /// </summary>
    public PluginReviewView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
