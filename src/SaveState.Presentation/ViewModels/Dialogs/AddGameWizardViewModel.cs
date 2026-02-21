using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class AddGameWizardViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly ILogger<AddGameWizardViewModel>? _logger;

    // Validation constants
    private const int MaxTitleLength = 200;
    private const int MaxPathLength = 260;
    private static readonly Regex InvalidCharsPattern = new Regex(@"[<>\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.Compiled);

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTitleValid))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private string _title = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPathValid))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
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

    [ObservableProperty]
    private string _validationError = string.Empty;

    /// <summary>
    /// Gets whether the title is valid.
    /// </summary>
    public bool IsTitleValid => 
        !string.IsNullOrWhiteSpace(Title) && 
        Title.Length <= MaxTitleLength &&
        !InvalidCharsPattern.IsMatch(Title);

    /// <summary>
    /// Gets whether the path is valid.
    /// </summary>
    public bool IsPathValid => 
        !string.IsNullOrWhiteSpace(Path) && 
        Path.Length <= MaxPathLength;

    public ObservableCollection<string> Platforms { get; } = new()
    {
        "PC",
        "Emulator",
        "Steam",
        "GOG",
        "Epic"
    };

    public AddGameWizardViewModel(IDialogService dialogService, ILogger<AddGameWizardViewModel>? logger = null)
    {
        _dialogService = dialogService;
        _logger = logger;
        UpdateValidation();
    }

    partial void OnTitleChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxTitleLength)
        {
            Title = value[..MaxTitleLength];
            return;
        }
        UpdateValidation();
    }

    partial void OnPathChanged(string value)
    {
        // Auto-truncate if exceeds max length
        if (value?.Length > MaxPathLength)
        {
            Path = value[..MaxPathLength];
            return;
        }
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
        var extensions = SelectedPlatform switch
        {
            "PC" => new[] { "exe", "lnk", "bat" },
            "Emulator" => new[] { "iso", "bin", "cue", "zip", "nes", "sfc", "gba", "md" },
            _ => new[] { "*" }
        };

        var result = await _dialogService.ShowFilePickerAsync($"Select {SelectedPlatform} Game", extensions);

        if (!string.IsNullOrEmpty(result))
        {
            Path = result;
        }
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
        // Final validation
        if (!IsTitleValid || !IsPathValid)
        {
            _logger?.LogWarning("Attempted to save wizard with invalid data");
            return;
        }

        var result = new AddGameResult(Title.Trim(), Path.Trim(), SelectedPlatform, ScanAutomatically);
        CloseDialog(result);
    }

    private void UpdateValidation()
    {
        ValidationError = string.Empty;

        // Simple validation
        switch (Step)
        {
            case 1:
                CanGoNext = !string.IsNullOrEmpty(SelectedPlatform);
                break;
            case 2:
                CanGoNext = IsPathValid;
                if (CanGoNext && !File.Exists(Path) && !Directory.Exists(Path))
                {
                    // Warn but don't block - file might be on removable media
                    ValidationError = "Warning: Path does not currently exist.";
                }
                // Auto-fill title from path if empty
                if (IsPathValid && string.IsNullOrEmpty(Title))
                {
                    Title = System.IO.Path.GetFileNameWithoutExtension(Path);
                }
                break;
            case 3:
                CanGoNext = IsTitleValid;
                if (!IsTitleValid)
                {
                    if (string.IsNullOrWhiteSpace(Title))
                        ValidationError = "Title is required.";
                    else if (Title.Length > MaxTitleLength)
                        ValidationError = $"Title must not exceed {MaxTitleLength} characters.";
                    else
                        ValidationError = "Title contains invalid characters.";
                }
                break;
        }
    }

    private void CloseDialog(AddGameResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}
