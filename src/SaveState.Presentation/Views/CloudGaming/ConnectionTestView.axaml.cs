using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.CloudGaming;

/// <summary>
/// Code-behind for the Connection Test view.
/// </summary>
public partial class ConnectionTestView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the ConnectionTestView.
    /// </summary>
    public ConnectionTestView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
