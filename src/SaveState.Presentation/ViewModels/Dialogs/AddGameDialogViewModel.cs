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

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
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

    [RelayCommand]
    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ValidationError = "Title is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Path))
        {
            ValidationError = "Path is required.";
            return;
        }

        /*
         * Note: Platform can be null if user wants "Unknown" or generic,
         * but usually for this app we strongly prefer a platform.
         * We'll send platform name or ID. AddGameResult expects string? Platform (name/id).
         */

        var result = new AddGameResult(
            Title,
            Path,
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
