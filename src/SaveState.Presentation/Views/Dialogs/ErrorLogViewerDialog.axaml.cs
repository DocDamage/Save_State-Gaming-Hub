using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// Error Log Viewer dialog.
/// Displays a searchable, filterable list of error log entries.
/// </summary>
public partial class ErrorLogViewerDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorLogViewerDialog"/> class.
    /// </summary>
    public ErrorLogViewerDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Handles pointer pressed event on error items to select them.
    /// </summary>
    private void OnErrorItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.DataContext is SaveState.Presentation.Models.Health.ErrorLogEntry entry)
        {
            if (DataContext is ErrorLogViewerDialogViewModel vm)
            {
                vm.SelectedError = entry;
            }
        }
    }
}
