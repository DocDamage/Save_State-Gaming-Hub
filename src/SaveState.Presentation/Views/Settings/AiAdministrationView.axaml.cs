using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Settings;

/// <summary>
/// View for AI Administration settings.
/// </summary>
public partial class AiAdministrationView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiAdministrationView"/> class.
    /// </summary>
    public AiAdministrationView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
