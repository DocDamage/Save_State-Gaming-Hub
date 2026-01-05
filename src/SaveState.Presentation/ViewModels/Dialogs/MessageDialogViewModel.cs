using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the message dialog.
/// </summary>
public partial class MessageDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _message;

    [ObservableProperty]
    private MessageDialogType _dialogType;

    public string Icon => DialogType switch
    {
        MessageDialogType.Information => "ℹ️",
        MessageDialogType.Warning => "⚠️",
        MessageDialogType.Error => "❌",
        _ => "ℹ️"
    };

    public MessageDialogViewModel(string title, string message, MessageDialogType dialogType)
    {
        _title = title;
        _message = message;
        _dialogType = dialogType;
    }

    [RelayCommand]
    private void Close()
    {
        // Close dialog
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
    }
}
