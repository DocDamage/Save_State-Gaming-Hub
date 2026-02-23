using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.CloudGaming;

/// <summary>
/// Code-behind for the Cloud Game Detail view.
/// </summary>
public partial class CloudGameDetailView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the CloudGameDetailView.
    /// </summary>
    public CloudGameDetailView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
