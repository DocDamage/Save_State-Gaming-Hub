using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for a simple text input dialog.
/// </summary>
public partial class TextInputDialogViewModel : ObservableObject
{
    private Action<string?>? _closeAction;

    [ObservableProperty]
    private string _title = "Input";

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _placeholder = "Enter text...";

    [ObservableProperty]
    private bool _isSensitive;

    [ObservableProperty]
    private bool _showSensitiveText;

    [ObservableProperty]
    private string _confirmText = "OK";

    [ObservableProperty]
    private string _cancelText = "Cancel";

    public char? PasswordChar => IsSensitive && !ShowSensitiveText ? '*' : null;

    partial void OnIsSensitiveChanged(bool value)
    {
        OnPropertyChanged(nameof(PasswordChar));
        if (!value)
        {
            ShowSensitiveText = false;
        }
    }

    partial void OnShowSensitiveTextChanged(bool value)
    {
        OnPropertyChanged(nameof(PasswordChar));
    }

    public void SetCloseAction(Action<string?> closeAction)
    {
        _closeAction = closeAction;
    }

    [RelayCommand]
    private void Confirm()
    {
        _closeAction?.Invoke(InputText);
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }
}
