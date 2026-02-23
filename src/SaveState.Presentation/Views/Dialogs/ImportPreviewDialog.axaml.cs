using Avalonia.Controls;
using Avalonia.Interactivity;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Dialog for previewing data import operations.
/// </summary>
public partial class ImportPreviewDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportPreviewDialog"/> class.
    /// </summary>
    public ImportPreviewDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the dialog with the view model.
    /// </summary>
    public void Initialize(ImportPreviewDialogViewModel viewModel)
    {
        DataContext = viewModel;

        viewModel.SetCloseAction(result =>
        {
            Close(result);
        });
    }
}
