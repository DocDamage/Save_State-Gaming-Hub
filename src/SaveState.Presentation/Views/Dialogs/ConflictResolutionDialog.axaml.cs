using Avalonia.Controls;
using Avalonia.Interactivity;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

public partial class ConflictResolutionDialog : Window
{
    public ConflictResolutionDialog()
    {
        InitializeComponent();
    }

    private void OnResolveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConflictResolutionDialogViewModel viewModel)
        {
            viewModel.ResolveAllCommand.Execute(null);
            Close(viewModel.Result);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
