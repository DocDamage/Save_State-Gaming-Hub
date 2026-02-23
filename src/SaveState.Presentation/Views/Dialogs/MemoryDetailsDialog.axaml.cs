using Avalonia.Controls;
using Avalonia.Interactivity;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Dialog for viewing conversation memory details.
/// </summary>
public partial class MemoryDetailsDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryDetailsDialog"/> class.
    /// </summary>
    public MemoryDetailsDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the close button click.
    /// </summary>
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MemoryDetailsDialogViewModel viewModel)
        {
            viewModel.CloseCommand.Execute(null);
        }
        Close(true);
    }
}
