using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Code-behind for the Plugin Install Dialog.
/// </summary>
public partial class PluginInstallDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the PluginInstallDialog class.
    /// </summary>
    public PluginInstallDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
