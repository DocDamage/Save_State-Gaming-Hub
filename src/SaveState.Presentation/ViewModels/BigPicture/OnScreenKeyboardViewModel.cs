using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text;

namespace SaveState.Presentation.ViewModels.BigPicture;

public partial class OnScreenKeyboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _placeholderText = "Enter text...";

    public event Action<string>? Completed;
    public event Action? Cancelled;

    [RelayCommand]
    private void KeyPressed(string key)
    {
        if (key == "⌫")
        {
            if (InputText.Length > 0)
                InputText = InputText.Substring(0, InputText.Length - 1);
        }
        else if (key == "Space")
        {
            InputText += " ";
        }
        else if (key == "Shift")
        {
            // Toggle shift logic could be added here
        }
        else
        {
            InputText += key;
        }
    }

    [RelayCommand]
    private void Submit()
    {
        Completed?.Invoke(InputText);
    }

    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke();
    }
}
