using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.RetroArch;

/// <summary>
/// Code-behind for the NetplayView.
/// </summary>
public partial class NetplayView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetplayView"/> class.
    /// </summary>
    public NetplayView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
