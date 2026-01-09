using Avalonia.Controls;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.Views.Dialogs;

public partial class AddGameDialog : Window
{
    public AddGameDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is AddGameDialogViewModel vm)
        {
            vm.SetCloseAction((result) => Close(result));
        }
    }
}
