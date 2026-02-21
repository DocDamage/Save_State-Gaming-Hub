using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.RetroArch;

/// <summary>
/// Code-behind for the RetroArchTabView.
/// </summary>
public partial class RetroArchTabView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetroArchTabView"/> class.
    /// </summary>
    public RetroArchTabView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
