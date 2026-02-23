using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Settings;

/// <summary>
/// View for API key management interface.
/// </summary>
public partial class ApiKeyManagerView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyManagerView"/> class.
    /// </summary>
    public ApiKeyManagerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
