using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Dialog window for the account connection wizard.
/// </summary>
public partial class AccountConnectionWizard : Window
{
    public AccountConnectionWizard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
