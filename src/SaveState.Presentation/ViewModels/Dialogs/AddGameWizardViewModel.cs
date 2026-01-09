using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class AddGameWizardViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private string _selectedPlatform = "PC";

    [ObservableProperty]
    private bool _scanAutomatically = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step1Visible))]
    [NotifyPropertyChangedFor(nameof(Step2Visible))]
    [NotifyPropertyChangedFor(nameof(Step3Visible))]
    private int _step = 1;

    public bool Step1Visible => Step == 1;
    public bool Step2Visible => Step == 2;
    public bool Step3Visible => Step == 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NextButtonText))]
    private bool _canGoNext;

    public string NextButtonText => Step == 3 ? "Finish" : "Next";

    public ObservableCollection<string> Platforms { get; } = new()
    {
        "PC",
        "Emulator",
        "Steam",
        "GOG",
        "Epic"
    };

    public AddGameWizardViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        UpdateValidation();
    }

    [RelayCommand]
    private void SelectPlatform(string platform)
    {
        SelectedPlatform = platform;
        Next();
    }

    [RelayCommand]
    private async Task BrowseFile()
    {
        // TODO: Use IDialogService to browse file
        // For now we will assume the user types it or we mock it
        // Ideally IDialogService needs a ShowFilePickerAsync method

        // Simulating file pick for now if service missing
        // var file = await _dialogService.ShowFilePickerAsync("Select Game Executable/ROM");
        // if (file != null) Path = file;
    }

    [RelayCommand]
    private void Next()
    {
        if (Step < 3)
        {
            Step++;
            UpdateValidation();
            OnPropertyChanged(nameof(NextButtonText));
        }
        else
        {
            Save();
        }
    }

    [RelayCommand]
    private void Back()
    {
        if (Step > 1)
        {
            Step--;
            UpdateValidation();
            OnPropertyChanged(nameof(NextButtonText));
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(null);
        }
    }

    private void Save()
    {
        var result = new AddGameResult(Title, Path, SelectedPlatform, ScanAutomatically);

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }

    partial void OnPathChanged(string value) => UpdateValidation();
    partial void OnTitleChanged(string value) => UpdateValidation();

    private void UpdateValidation()
    {
        // Simple validation
        switch (Step)
        {
            case 1:
                CanGoNext = !string.IsNullOrEmpty(SelectedPlatform);
                break;
            case 2:
                CanGoNext = !string.IsNullOrEmpty(Path);
                // Auto-fill title from path if empty
                if (!string.IsNullOrEmpty(Path) && string.IsNullOrEmpty(Title))
                {
                    Title = System.IO.Path.GetFileNameWithoutExtension(Path);
                }
                break;
            case 3:
                CanGoNext = !string.IsNullOrEmpty(Title);
                break;
        }
    }
}
