using Avalonia.Controls;
using Avalonia.Interactivity;
using SaveState.Presentation.ViewModels.Library;

namespace SaveState.Presentation.Views.Library;

/// <summary>
/// Dialog for importing game libraries from various platforms.
/// </summary>
public partial class LibraryImportDialog : Window
{
    public LibraryImportDialog()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
