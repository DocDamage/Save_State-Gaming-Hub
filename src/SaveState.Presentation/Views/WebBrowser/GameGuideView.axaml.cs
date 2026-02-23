using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.WebBrowser;

/// <summary>
/// Code-behind for the game guide view.
/// </summary>
public partial class GameGuideView : UserControl
{
    public GameGuideView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
