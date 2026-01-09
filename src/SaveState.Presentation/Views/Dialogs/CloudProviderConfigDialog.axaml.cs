using Avalonia.Controls;
using Avalonia.Interactivity;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

public partial class CloudProviderConfigDialog : Window
{
    public CloudProviderConfigDialog()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CloudProviderConfigDialogViewModel viewModel)
        {
            viewModel.SaveCommand.Execute(null);
            Close(viewModel.Result);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
