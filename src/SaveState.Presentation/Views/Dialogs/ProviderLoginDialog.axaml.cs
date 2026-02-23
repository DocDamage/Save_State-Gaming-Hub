using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Provider login dialog for connecting cloud gaming accounts.
/// </summary>
public partial class ProviderLoginDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the ProviderLoginDialog.
    /// </summary>
    public ProviderLoginDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
