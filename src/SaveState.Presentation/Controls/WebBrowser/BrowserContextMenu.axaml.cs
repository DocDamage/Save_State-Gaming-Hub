using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Controls.WebBrowser;

/// <summary>
/// Custom context menu for the web browser.
/// </summary>
public partial class BrowserContextMenu : ContextMenu
{
    public BrowserContextMenu()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
