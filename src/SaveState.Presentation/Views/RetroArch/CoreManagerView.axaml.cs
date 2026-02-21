using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.RetroArch;

/// <summary>
/// Code-behind for the CoreManagerView.
/// </summary>
public partial class CoreManagerView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoreManagerView"/> class.
    /// </summary>
    public CoreManagerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
