using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the confirmation dialog.
/// </summary>
public partial class ConfirmationDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _message;

    [ObservableProperty]
    private string _confirmText;

    [ObservableProperty]
    private string _cancelText;

    public ConfirmationDialogViewModel(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
    {
        _title = title;
        _message = message;
        _confirmText = confirmText;
        _cancelText = cancelText;
    }

    [RelayCommand]
    private void Confirm()
    {
        // Close dialog with true result
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(true);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        // Close dialog with false result
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(false);
        }
    }
}
