using Avalonia.Controls;
using Avalonia.Interactivity;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Dialog for configuring LLM provider settings.
/// </summary>
public partial class ProviderConfigDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderConfigDialog"/> class.
    /// </summary>
    public ProviderConfigDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the cancel button click.
    /// </summary>
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProviderConfigDialogViewModel viewModel)
        {
            viewModel.CancelCommand.Execute(null);
        }
        Close(false);
    }

    /// <summary>
    /// Handles the save button click.
    /// </summary>
    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProviderConfigDialogViewModel viewModel)
        {
            await viewModel.SaveCommand.ExecuteAsync(null);
            if (viewModel.Result == true)
            {
                Close(true);
            }
        }
    }
}
