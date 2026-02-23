using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.QuickActions;

/// <summary>
/// Floating quick action bar with common actions.
/// </summary>
public partial class QuickActionBar : UserControl
{
    /// <summary>
    /// Initializes a new instance of the QuickActionBar class.
    /// </summary>
    public QuickActionBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
