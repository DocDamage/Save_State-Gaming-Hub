using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for a simple text input dialog.
/// </summary>
public partial class TextInputDialogViewModel : ObservableObject
{
    private Action<string?>? _closeAction;

    // Validation constants
    private const int MaxInputLength = 500;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    private string _title = "Input";

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInputValid))]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    [NotifyPropertyChangedFor(nameof(ValidationMessage))]
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

    [ObservableProperty]
    private bool _isRequired = true;

    /// <summary>
    /// Gets whether the current input is valid.
    /// </summary>
    public bool IsInputValid
    {
        get
        {
            if (IsRequired && string.IsNullOrWhiteSpace(InputText))
                return false;
            if (InputText?.Length > MaxInputLength)
                return false;
            if (!string.IsNullOrEmpty(InputText) && InvalidCharsPattern.IsMatch(InputText))
                return false;
            return true;
        }
    }

    /// <summary>
    /// Gets whether the confirm button should be enabled.
    /// </summary>
    public bool CanConfirm => IsInputValid;

    /// <summary>
    /// Gets the validation message for the current input.
    /// </summary>
    public string? ValidationMessage
    {
        get
        {
            if (IsRequired && string.IsNullOrWhiteSpace(InputText))
                return "Input is required.";
            if (InputText?.Length > MaxInputLength)
                return $"Input must not exceed {MaxInputLength} characters.";
            if (!string.IsNullOrEmpty(InputText) && InvalidCharsPattern.IsMatch(InputText))
                return "Input contains invalid characters.";
            return null;
        }
    }

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

    partial void OnInputTextChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxInputLength)
        {
            InputText = value[..MaxInputLength];
            return;
        }
    }

    public void SetCloseAction(Action<string?> closeAction)
    {
        _closeAction = closeAction;
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        // Sanitize and trim input before returning
        var sanitized = InputText?.Trim() ?? string.Empty;
        _closeAction?.Invoke(sanitized);
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }
}
