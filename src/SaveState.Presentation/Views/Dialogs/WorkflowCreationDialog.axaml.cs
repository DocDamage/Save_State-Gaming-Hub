using Avalonia.Controls;
using Avalonia.Interactivity;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

public partial class WorkflowCreationDialog : Window
{
    public WorkflowCreationDialog()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is WorkflowCreationDialogViewModel vm)
        {
            vm.Save();
            Close(vm.Result);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
