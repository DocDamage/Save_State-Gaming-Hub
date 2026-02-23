using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Settings;

namespace SaveState.Presentation.Views.Settings;

/// <summary>
/// View for the Performance Dashboard.
/// Provides real-time monitoring of system performance metrics and game statistics.
/// </summary>
public partial class PerformanceDashboardView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceDashboardView"/> class.
    /// </summary>
    public PerformanceDashboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
