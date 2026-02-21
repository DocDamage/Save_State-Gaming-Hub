using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Health;

/// <summary>
/// Code-behind for the HealthMonitorView.
/// </summary>
public partial class HealthMonitorView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthMonitorView"/> class.
    /// </summary>
    public HealthMonitorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
