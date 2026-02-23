using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Settings;

/// <summary>
/// System Health Dashboard view.
/// Displays overall system health, database status, API statuses, cache stats, and system resources.
/// </summary>
public partial class SystemHealthView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemHealthView"/> class.
    /// </summary>
    public SystemHealthView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
