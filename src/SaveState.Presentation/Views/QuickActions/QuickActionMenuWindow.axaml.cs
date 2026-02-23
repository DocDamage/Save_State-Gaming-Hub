using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.QuickActions;

/// <summary>
/// Window container for the quick action menu.
/// </summary>
public partial class QuickActionMenuWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the QuickActionMenuWindow class.
    /// </summary>
    public QuickActionMenuWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
