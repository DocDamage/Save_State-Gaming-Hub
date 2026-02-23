using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.WebBrowser;

/// <summary>
/// Bookmarks manager view with folders and bookmark management.
/// </summary>
public partial class BookmarksManagerView : UserControl
{
    public BookmarksManagerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
