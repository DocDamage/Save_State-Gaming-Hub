using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Dialog for confirming mobile device pairing requests.
/// </summary>
public partial class PairingDialog : Window
{
    public PairingDialog()
    {
        InitializeComponent();
    }

    public PairingDialog(PairingDialogViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
