using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.WebBrowser;

/// <summary>
/// Code-behind for the documentation browser view.
/// </summary>
public partial class DocumentationBrowserView : UserControl
{
    public DocumentationBrowserView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
