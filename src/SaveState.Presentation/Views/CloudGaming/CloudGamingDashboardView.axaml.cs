using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.CloudGaming;

/// <summary>
/// Code-behind for the Cloud Gaming Dashboard view.
/// </summary>
public partial class CloudGamingDashboardView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the CloudGamingDashboardView.
    /// </summary>
    public CloudGamingDashboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
