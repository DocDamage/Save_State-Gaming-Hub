using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
}
