using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.RetroArch.Commands;
using SaveState.Application.RetroArch.Queries;
using SaveState.Core.RetroArch;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// ViewModel for RetroArch integration.
/// </summary>
public partial class RetroArchViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly INotificationService _notificationService;
    private readonly ILogger<RetroArchViewModel> _logger;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRetroArchDetected;

    [ObservableProperty]
    private string _retroArchPath = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Detecting RetroArch...";

    [ObservableProperty]
    private int _gamesCount;

    [ObservableProperty]
    private int _installedCoresCount;

    [ObservableProperty]
    private RetroArchGame? _selectedGame;

    [ObservableProperty]
    private RetroArchCore? _selectedCore;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<RetroArchGame> Games { get; } = new();
    public ObservableCollection<RetroArchCore> InstalledCores { get; } = new();
    public ObservableCollection<RetroArchCore> AvailableCores { get; } = new();
    public ObservableCollection<RetroArchGame> FilteredGames { get; } = new();

    public RetroArchViewModel(
        IMediator mediator,
        INotificationService notificationService,
        ILogger<RetroArchViewModel> logger)
    {
        _mediator = mediator;
        _notificationService = notificationService;
        _logger = logger;

        // Initialize collections immediately to prevent null reference exceptions
        Games = new ObservableCollection<RetroArchGame>();
        InstalledCores = new ObservableCollection<RetroArchCore>();
        AvailableCores = new ObservableCollection<RetroArchCore>();
        FilteredGames = new ObservableCollection<RetroArchGame>();
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Detecting RetroArch installation...";

            // Load all data
            await LoadGamesAsync();
            await LoadInstalledCoresAsync();
            await LoadAvailableCoresAsync();

            IsRetroArchDetected = Games.Count > 0 || InstalledCores.Count > 0;

            if (IsRetroArchDetected)
            {
                StatusMessage = $"RetroArch detected - {GamesCount} games, {InstalledCoresCount} cores";
            }
            else
            {
                StatusMessage = "RetroArch not detected. Please install RetroArch or specify the installation path.";
            }
        }
        catch (Exception ex)
        {
            LogInitializationFailed(_logger, ex);
            StatusMessage = "Failed to detect RetroArch. See logs for details.";
            _notificationService.ShowError("Failed to initialize RetroArch integration");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Detecting RetroArch installation...";

            // Load all data
            await LoadGamesAsync();
            await LoadInstalledCoresAsync();
            await LoadAvailableCoresAsync();

            IsRetroArchDetected = Games.Count > 0 || InstalledCores.Count > 0;

            if (IsRetroArchDetected)
            {
                StatusMessage = $"RetroArch detected - {GamesCount} games, {InstalledCoresCount} cores";
            }
            else
            {
                StatusMessage = "RetroArch not detected. Please install RetroArch or specify the installation path.";
            }
        }
        catch (Exception ex)
        {
            LogLoadDataFailed(_logger, ex);
            StatusMessage = "Failed to detect RetroArch. See logs for details.";
            _notificationService.ShowError("Failed to load RetroArch data");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadGamesAsync()
    {
        try
        {
            IsLoading = true;
            var result = await _mediator.Send(new GetRetroArchGamesQuery());

            if (result.IsSuccess && result.Value != null)
            {
                Games.Clear();
                FilteredGames.Clear();

                foreach (var game in result.Value)
                {
                    Games.Add(game);
                    FilteredGames.Add(game);
                }

                GamesCount = Games.Count;
                LogGamesLoaded(_logger, GamesCount);
            }
            else
            {
                LogLoadGamesFailed(_logger, result.Error ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            LogGamesError(_logger, ex);
            _notificationService.ShowError("Failed to load games");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadInstalledCoresAsync()
    {
        try
        {
            var result = await _mediator.Send(new GetInstalledCoresQuery());

            if (result.IsSuccess && result.Value != null)
            {
                InstalledCores.Clear();
                foreach (var core in result.Value)
                {
                    InstalledCores.Add(core);
                }

                InstalledCoresCount = InstalledCores.Count;
                LogInstalledCoresLoaded(_logger, InstalledCoresCount);
            }
        }
        catch (Exception ex)
        {
            LogInstalledCoresError(_logger, ex);
        }
    }

    [RelayCommand]
    private async Task LoadAvailableCoresAsync()
    {
        try
        {
            // Don't use ConfigureAwait(false) - we need to stay on UI thread to modify ObservableCollection
            var result = await _mediator.Send(new GetAvailableCoresQuery());

            if (result.IsSuccess && result.Value != null)
            {
                AvailableCores.Clear();
                foreach (var core in result.Value)
                {
                    AvailableCores.Add(core);
                }

                LogAvailableCoresLoaded(_logger, AvailableCores.Count);
            }
            else
            {
                LogLoadAvailableCoresFailed(_logger, result.Error ?? "Unknown error");
                // Set an empty list to prevent UI binding issues
                AvailableCores.Clear();
            }
        }
        catch (Exception ex)
        {
            LogAvailableCoresError(_logger, ex);
            _notificationService.ShowError("Failed to load available cores");
            // Ensure the collection is in a valid state even on error
            AvailableCores.Clear();
        }
    }

    [RelayCommand]
    private async Task ImportGamesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Importing RetroArch games into library...";

            var result = await _mediator.Send(new ImportRetroArchGamesCommand());

            if (result.IsSuccess)
            {
                var importedCount = result.Value;
                _notificationService.ShowSuccess($"Successfully imported {importedCount} games into your library!");
                StatusMessage = $"Imported {importedCount} games";
                LogGamesImported(_logger, importedCount);
            }
            else
            {
                _notificationService.ShowError($"Failed to import games: {result.Error}");
                LogImportGamesFailed(_logger, result.Error ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            LogImportError(_logger, ex);
            _notificationService.ShowError("Failed to import games");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LaunchGameAsync()
    {
        if (SelectedGame == null)
        {
            _notificationService.ShowWarning("Please select a game to launch");
            return;
        }

        await LaunchSpecificGameAsync(SelectedGame);
    }

    [RelayCommand]
    private async Task LaunchSpecificGameAsync(RetroArchGame game)
    {
        try
        {
            StatusMessage = $"Launching {game.Label}...";

            var result = await _mediator.Send(new LaunchRetroArchGameCommand(
                game.Path,
                game.CorePath));

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Launched {game.Label}");
                LogGameLaunched(_logger, game.Label);
            }
            else
            {
                _notificationService.ShowError($"Failed to launch game: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            LogLaunchError(_logger, ex);
            _notificationService.ShowError("Failed to launch game");
        }
    }

    [RelayCommand]
    private async Task InstallCoreAsync(string coreName)
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"Installing core: {coreName}...";

            var result = await _mediator.Send(new InstallCoreCommand(coreName));

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Successfully installed {coreName}");
                await LoadInstalledCoresAsync();
            }
            else
            {
                _notificationService.ShowError($"Failed to install core: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            LogInstallCoreError(_logger, ex);
            _notificationService.ShowError("Failed to install core");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UpdateCoreAsync(string coreName)
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"Updating core: {coreName}...";

            var result = await _mediator.Send(new UpdateCoreCommand(coreName));

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Successfully updated {coreName}");
                await LoadInstalledCoresAsync();
            }
            else
            {
                _notificationService.ShowError($"Failed to update core: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            LogUpdateCoreError(_logger, ex);
            _notificationService.ShowError("Failed to update core");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SyncSavesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Syncing saves...";

            var result = await _mediator.Send(new SyncSavesCommand());

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess("Saves synced successfully");
            }
            else
            {
                _notificationService.ShowError($"Failed to sync saves: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            LogSyncSavesError(_logger, ex);
            _notificationService.ShowError("Failed to sync saves");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SearchGames()
    {
        FilteredGames.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? Games
            : Games.Where(g => g.Label.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var game in filtered)
        {
            FilteredGames.Add(game);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        SearchGames();
    }

    #region Logging

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to initialize RetroArch integration")]
    static partial void LogInitializationFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to load RetroArch data")]
    static partial void LogLoadDataFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Loaded {Count} RetroArch games")]
    static partial void LogGamesLoaded(ILogger logger, int count);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Failed to load RetroArch games: {Error}")]
    static partial void LogLoadGamesFailed(ILogger logger, string error);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Error loading RetroArch games")]
    static partial void LogGamesError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Loaded {Count} installed cores")]
    static partial void LogInstalledCoresLoaded(ILogger logger, int count);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error, Message = "Error loading installed cores")]
    static partial void LogInstalledCoresError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Loaded {Count} available cores")]
    static partial void LogAvailableCoresLoaded(ILogger logger, int count);

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "Failed to load available cores: {Error}")]
    static partial void LogLoadAvailableCoresFailed(ILogger logger, string error);

    [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "Error loading available cores")]
    static partial void LogAvailableCoresError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Imported {Count} RetroArch games")]
    static partial void LogGamesImported(ILogger logger, int count);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Failed to import games: {Error}")]
    static partial void LogImportGamesFailed(ILogger logger, string error);

    [LoggerMessage(EventId = 13, Level = LogLevel.Error, Message = "Error importing games")]
    static partial void LogImportError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "Launched game: {Game}")]
    static partial void LogGameLaunched(ILogger logger, string game);

    [LoggerMessage(EventId = 15, Level = LogLevel.Error, Message = "Error launching game")]
    static partial void LogLaunchError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 16, Level = LogLevel.Error, Message = "Error installing core")]
    static partial void LogInstallCoreError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 17, Level = LogLevel.Error, Message = "Error updating core")]
    static partial void LogUpdateCoreError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 18, Level = LogLevel.Error, Message = "Error syncing saves")]
    static partial void LogSyncSavesError(ILogger logger, Exception ex);

    #endregion
}
