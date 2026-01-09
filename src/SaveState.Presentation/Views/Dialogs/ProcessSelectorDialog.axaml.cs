using Avalonia.Controls;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

public partial class ProcessSelectorDialog : Window
{
    public ProcessSelectorDialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ProcessSelectorDialogViewModel vm)
        {
            vm.SetCloseAction((result) => Close(result));
        }
    }
}
