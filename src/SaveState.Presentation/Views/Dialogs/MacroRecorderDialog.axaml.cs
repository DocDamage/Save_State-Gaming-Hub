using Avalonia.Controls;
using Avalonia.Interactivity;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

public partial class MacroRecorderDialog : Window
{
    public MacroRecorderDialog()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MacroRecorderDialogViewModel vm)
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
