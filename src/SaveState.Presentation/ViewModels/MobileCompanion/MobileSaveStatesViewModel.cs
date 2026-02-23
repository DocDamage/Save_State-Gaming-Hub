using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.Mobile;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.MobileCompanion;

/// <summary>
/// ViewModel for managing save states from the mobile companion app.
/// Allows browsing, creating, loading, and sharing save states remotely.
/// </summary>
public partial class MobileSaveStatesViewModel : ObservableObject
{
    private readonly ILogger<MobileSaveStatesViewModel> _logger;
    private readonly IMobileCompanionService? _companionService;

    [ObservableProperty]
    private ObservableCollection<SaveStateInfo> _saveStates = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableGames = new();

    [ObservableProperty]
    private string _selectedGame = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private SaveStateInfo? _selectedSaveState;

    [ObservableProperty]
    private bool _isSaveStateDetailOpen;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SaveStateInfo> _filteredSaveStates = new();

    [ObservableProperty]
    private string _newSaveStateName = string.Empty;

    [ObservableProperty]
    private string _newSaveStateDescription = string.Empty;

    public MobileSaveStatesViewModel(
        ILogger<MobileSaveStatesViewModel> logger,
        IMobileCompanionService? companionService = null)
    {
        _logger = logger;
        _companionService = companionService;
        _ = InitializeAsync();
    }

    /// <summary>
    /// Initializes the view model and loads available games
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            await LoadAvailableGamesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize save states view");
        }
    }

    /// <summary>
    /// Loads save states for the selected game
    /// </summary>
    [RelayCommand]
    private async Task LoadSaveStatesAsync()
    {
        try
        {
            IsLoading = true;
            SaveStates.Clear();
            FilteredSaveStates.Clear();

            _logger.LogInformation("Loading save states for {Game}", SelectedGame);

            if (_companionService is not null)
            {
                // NOTE: This is a demo implementation. Replace with actual service call.
            }
            else
            {
                // Demo data
                await LoadDemoSaveStatesAsync();
            }

            ApplySearchFilter();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load save states");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a new save state on the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task CreateSaveStateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewSaveStateName))
            {
                return;
            }

            _logger.LogInformation("Creating save state: {Name}", NewSaveStateName);

            IsLoading = true;

            if (_companionService is not null)
            {
                // NOTE: This is a demo implementation. Replace with actual service call.
                await Task.Delay(1000); // Simulate network delay
            }

            // Add to local collection
            var newSaveState = new SaveStateInfo
            {
                Id = Guid.NewGuid().ToString(),
                GameId = SelectedGame,
                GameTitle = SelectedGame,
                Name = NewSaveStateName,
                Description = NewSaveStateDescription,
                CreatedAt = DateTime.Now,
                IsCloudSynced = false,
                FileSize = 1024 * 1024 * 15 // 15MB demo
            };

            SaveStates.Insert(0, newSaveState);
            ApplySearchFilter();

            // Clear input
            NewSaveStateName = string.Empty;
            NewSaveStateDescription = string.Empty;

            _logger.LogInformation("Save state created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create save state");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads a save state on the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task LoadSaveStateAsync(SaveStateInfo? saveState)
    {
        if (saveState is null) return;

        try
        {
            _logger.LogInformation("Loading save state: {Name}", saveState.Name);
            IsLoading = true;

            if (_companionService is not null)
            {
                // NOTE: This is a demo implementation. Replace with actual service call.
            }

            await Task.Delay(500); // Simulate operation

            _logger.LogInformation("Save state loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load save state");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Deletes a save state from the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task DeleteSaveStateAsync(SaveStateInfo? saveState)
    {
        if (saveState is null) return;

        try
        {
            _logger.LogInformation("Deleting save state: {Name}", saveState.Name);

            if (_companionService is not null)
            {
                // NOTE: This is a demo implementation. Replace with actual service call.
            }

            SaveStates.Remove(saveState);
            ApplySearchFilter();

            if (SelectedSaveState == saveState)
            {
                SelectedSaveState = null;
                IsSaveStateDetailOpen = false;
            }

            _logger.LogInformation("Save state deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete save state");
        }
    }

    /// <summary>
    /// Shares a save state via cloud sync
    /// </summary>
    [RelayCommand]
    private async Task ShareSaveStateAsync(SaveStateInfo? saveState)
    {
        if (saveState is null) return;

        try
        {
            _logger.LogInformation("Sharing save state: {Name}", saveState.Name);

            if (_companionService is not null)
            {
                // NOTE: This is a demo implementation. Replace with actual service call.
            }

            // Show share options (native mobile share sheet)
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share save state");
        }
    }

    /// <summary>
    /// Opens the save state detail view
    /// </summary>
    [RelayCommand]
    private void OpenSaveStateDetail(SaveStateInfo? saveState)
    {
        if (saveState is null) return;

        SelectedSaveState = saveState;
        IsSaveStateDetailOpen = true;
    }

    /// <summary>
    /// Closes the save state detail view
    /// </summary>
    [RelayCommand]
    private void CloseSaveStateDetail()
    {
        IsSaveStateDetailOpen = false;
        SelectedSaveState = null;
    }

    /// <summary>
    /// Refreshes the save states list
    /// </summary>
    [RelayCommand]
    private async Task RefreshSaveStatesAsync()
    {
        await LoadSaveStatesAsync();
    }

    /// <summary>
    /// Searches save states by name
    /// </summary>
    [RelayCommand]
    private void SearchSaveStates()
    {
        ApplySearchFilter();
    }

    /// <summary>
    /// Clears the search query
    /// </summary>
    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        ApplySearchFilter();
    }

    /// <summary>
    /// Navigates back to the dashboard
    /// </summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        // Navigation would happen here
        await Task.CompletedTask;
    }

    /// <summary>
    /// Loads available games with save states
    /// </summary>
    private async Task LoadAvailableGamesAsync()
    {
        try
        {
            AvailableGames.Clear();

            // Demo data
            var games = new[] { "Elden Ring", "Hades II", "Baldur's Gate 3", "Cyberpunk 2077" };
            foreach (var game in games)
            {
                AvailableGames.Add(game);
            }

            if (AvailableGames.Count > 0)
            {
                SelectedGame = AvailableGames[0];
                await LoadSaveStatesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load available games");
        }
    }

    /// <summary>
    /// Loads demo save states
    /// </summary>
    private async Task LoadDemoSaveStatesAsync()
    {
        var demoStates = new[]
        {
            new SaveStateInfo
            {
                Id = "1",
                GameId = SelectedGame,
                GameTitle = SelectedGame,
                Name = "Boss Fight - Phase 2",
                Description = "Just before the final boss",
                CreatedAt = DateTime.Now.AddHours(-2),
                IsCloudSynced = true,
                FileSize = 1024 * 1024 * 12
            },
            new SaveStateInfo
            {
                Id = "2",
                GameId = SelectedGame,
                GameTitle = SelectedGame,
                Name = "Exploring Caelid",
                Description = "Found a secret area",
                CreatedAt = DateTime.Now.AddDays(-1),
                IsCloudSynced = true,
                FileSize = 1024 * 1024 * 10
            },
            new SaveStateInfo
            {
                Id = "3",
                GameId = SelectedGame,
                GameTitle = SelectedGame,
                Name = "Character Build - Mage",
                Description = "Full sorcery build at level 80",
                CreatedAt = DateTime.Now.AddDays(-3),
                IsCloudSynced = false,
                FileSize = 1024 * 1024 * 15
            },
            new SaveStateInfo
            {
                Id = "4",
                GameId = SelectedGame,
                GameTitle = SelectedGame,
                Name = "New Game+ Start",
                Description = "Beginning of second playthrough",
                CreatedAt = DateTime.Now.AddDays(-7),
                IsCloudSynced = true,
                FileSize = 1024 * 1024 * 8
            }
        };

        foreach (var state in demoStates)
        {
            SaveStates.Add(state);
        }
    }

    /// <summary>
    /// Applies the search filter to the save states list
    /// </summary>
    private void ApplySearchFilter()
    {
        FilteredSaveStates.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchQuery)
            ? SaveStates
            : SaveStates.Where(s =>
                s.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));

        foreach (var state in filtered)
        {
            FilteredSaveStates.Add(state);
        }
    }
}
