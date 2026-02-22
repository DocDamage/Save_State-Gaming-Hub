using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SaveState.Presentation.Views.Settings;

/// <summary>
/// Dialog for configuring automatic backup settings.
/// </summary>
public partial class AutoBackupConfigDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoBackupConfigDialog"/> class.
    /// </summary>
    public AutoBackupConfigDialog()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
