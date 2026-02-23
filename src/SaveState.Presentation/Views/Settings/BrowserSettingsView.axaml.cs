using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Settings;

/// <summary>
/// Browser settings view with sections for general, privacy, security,
/// downloads, appearance, and advanced settings.
/// </summary>
public partial class BrowserSettingsView : UserControl
{
    public BrowserSettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
