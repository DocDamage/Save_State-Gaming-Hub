using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Settings;

/// <summary>
/// System Health Dashboard view.
/// Displays system status, database health, external API status,
/// cache statistics, and recent errors.
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
}
