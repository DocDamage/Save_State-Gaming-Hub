using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Settings;

/// <summary>
/// View for accessibility settings.
/// </summary>
public partial class AccessibilitySettingsView : UserControl
{
    public AccessibilitySettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
