using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Settings;

/// <summary>
/// View for role management interface.
/// </summary>
public partial class RoleManagementView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleManagementView"/> class.
    /// </summary>
    public RoleManagementView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
