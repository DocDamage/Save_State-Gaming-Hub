using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.WebBrowser;

/// <summary>
/// Custom new tab page with shortcuts and recently visited sites.
/// </summary>
public partial class NewTabPage : UserControl
{
    public NewTabPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
