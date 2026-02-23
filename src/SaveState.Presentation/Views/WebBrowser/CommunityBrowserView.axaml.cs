using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.WebBrowser;

/// <summary>
/// Code-behind for the community browser view.
/// </summary>
public partial class CommunityBrowserView : UserControl
{
    public CommunityBrowserView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
