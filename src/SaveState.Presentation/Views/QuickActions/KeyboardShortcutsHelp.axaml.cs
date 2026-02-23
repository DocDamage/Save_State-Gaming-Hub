using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.QuickActions;

/// <summary>
/// Help window showing all available keyboard shortcuts.
/// </summary>
public partial class KeyboardShortcutsHelp : Window
{
    /// <summary>
    /// Initializes a new instance of the KeyboardShortcutsHelp class.
    /// </summary>
    public KeyboardShortcutsHelp()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
