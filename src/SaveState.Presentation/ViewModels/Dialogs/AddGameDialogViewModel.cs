using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class AddGameDialogViewModel : ObservableObject
{
    private readonly IPlatformRepository _platformRepository;
    private readonly IDialogService _dialogService;
    private Action<AddGameResult?>? _closeAction;

    // Validation constants
    private const int MaxTitleLength = 200;
    private const int MaxPathLength = 260;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTitleValid))]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _title = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPathValid))]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string _path = string.Empty;

    [ObservableProperty]
    private Platform? _selectedPlatform;

    [ObservableProperty]
    private ObservableCollection<Platform> _availablePlatforms = new();

    [ObservableProperty]
    private bool _scanAutomatically = true;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string _validationError = string.Empty;

    /// <summary>
    /// Gets whether the title is valid.
    /// </summary>
    public bool IsTitleValid => 
        !string.IsNullOrWhiteSpace(Title) && 
        Title.Length <= MaxTitleLength;

    /// <summary>
    /// Gets whether the path is valid.
    /// </summary>
    public bool IsPathValid => 
        !string.IsNullOrWhiteSpace(Path) && 
        Path.Length <= MaxPathLength;

    /// <summary>
    /// Gets whether the confirm button should be enabled.
    /// </summary>
    public bool CanConfirm => IsTitleValid && IsPathValid && !IsLoading;

    public AddGameDialogViewModel(
        IPlatformRepository platformRepository,
        IDialogService dialogService)
    {
        _platformRepository = platformRepository;
        _dialogService = dialogService;

        // Don't call async method from constructor - use InitializeAsync instead
    }

    public void SetCloseAction(Action<AddGameResult?> closeAction)
    {
        _closeAction = closeAction;
    }

    /// <summary>
    /// Initialize the dialog asynchronously. Should be called after construction.
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadPlatformsAsync();
    }

    private async Task LoadPlatformsAsync()
    {
        IsLoading = true;
        try
        {
            var platforms = await _platformRepository.GetAllAsync();
            AvailablePlatforms = new ObservableCollection<Platform>(platforms.OrderBy(p => p.Name.Value));

            if (AvailablePlatforms.Any())
            {
                SelectedPlatform = AvailablePlatforms.First();
            }
        }
        catch (Exception ex)
        {
            // Log exception - prevents application crash
            System.Diagnostics.Debug.WriteLine($"Error loading platforms: {ex}");
            AvailablePlatforms = new ObservableCollection<Platform>();
            ValidationError = "Failed to load platforms. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BrowsePathAsync()
    {
        // For game folders
        var path = await _dialogService.ShowFolderPickerAsync("Select Game Folder");
        if (!string.IsNullOrEmpty(path))
        {
            Path = path;
            // Try to guess title from folder name if empty
            if (string.IsNullOrWhiteSpace(Title))
            {
                Title = System.IO.Path.GetFileName(Path);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        if (!CanConfirm)
        {
            if (!IsTitleValid)
                ValidationError = $"Title is required and must not exceed {MaxTitleLength} characters.";
            else if (!IsPathValid)
                ValidationError = $"Path is required and must not exceed {MaxPathLength} characters.";
            return;
        }

        // Sanitize inputs
        var sanitizedTitle = Title.Trim();
        var sanitizedPath = Path.Trim();

        var result = new AddGameResult(
            sanitizedTitle,
            sanitizedPath,
            SelectedPlatform?.Id.ToString(),
            ScanAutomatically);

        _closeAction?.Invoke(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }
}
