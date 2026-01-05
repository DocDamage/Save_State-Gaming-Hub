using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.SaveStates.Queries;
using SaveState.Core.Common.ValueObjects;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using SaveState.Application.SaveStates.Commands;
using System.Threading.Tasks;
using SaveState.Presentation.Services;
using SaveState.Application.CloudServices.Queries;
using SaveState.Application.CloudServices.Commands;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

/// <summary>
/// View model for the Game Save States tab.
/// </summary>
public partial class GameSaveStatesTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private readonly ILogger<GameSaveStatesTabViewModel> _logger;

    [ObservableProperty]
    private string _saveStatesCountText = "0 save states";

    [ObservableProperty]
    private string _currentBranchName = "main";

    [ObservableProperty]
    private int _currentBranchSaveCount;

    [ObservableProperty]
    private int _totalSaveCount;

    [ObservableProperty]
    private int _autoSaveCount;

    [ObservableProperty]
    private string _totalSizeText = "0 MB";

    [ObservableProperty]
    private string _lastBackupText = "Never";

    [ObservableProperty]
    private bool _autoSaveEnabled = true;

    [ObservableProperty]
    private ObservableCollection<string> _autoSaveIntervals = new() { "1 min", "5 min", "15 min", "30 min", "1 hour" };

    [ObservableProperty]
    private string _selectedAutoSaveInterval = "5 min";

    [ObservableProperty]
    private ObservableCollection<GameSaveStateViewModel> _saveStates = new();

    public GameSaveStatesTabViewModel(
        IMediator mediator,
        IDialogService dialogService,
        ILogger<GameSaveStatesTabViewModel> logger)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _logger = logger;
    }

    private GameId? _currentGameId;

    public async Task LoadDataAsync(GameId gameId)
    {
        _currentGameId = gameId;
        try
        {
            // Load save states from backend
            var query = new GetSaveStatesQuery(gameId.Value);
            var result = await _mediator.Send(query).ConfigureAwait(false);

            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to load save states for game {GameId}: {Error}", gameId, result.Error);
                SaveStatesCountText = "0 save states";
                TotalSaveCount = 0;
                return;
            }

            var saveStates = result.Value;
            TotalSaveCount = saveStates.Count;
            SaveStatesCountText = $"{TotalSaveCount} save state{(TotalSaveCount == 1 ? "" : "s")}";

            // Calculate statistics
            // Note: BranchName functionality not yet implemented in SaveState entity
            CurrentBranchSaveCount = saveStates.Count; // All saves in current branch for now
            AutoSaveCount = saveStates.Count(s => s.IsAutoSave);

            // Calculate total size
            var totalSizeBytes = saveStates.Sum(s => s.FileSizeBytes);
            var totalSizeMB = (double)totalSizeBytes / (1024.0 * 1024.0);
            if (totalSizeMB < 1024)
                TotalSizeText = $"{totalSizeMB:F1} MB";
            else
                TotalSizeText = $"{totalSizeMB / 1024.0:F2} GB";

            // Find most recent global backup
            var backupQuery = new GetBackupHistoryQuery();
            var backupResult = await _mediator.Send(backupQuery).ConfigureAwait(false);

            if (backupResult.IsSuccess && backupResult.Value.Any())
            {
                var mostRecent = backupResult.Value.OrderByDescending(b => b.CreatedAt).First();
                var timeSince = DateTime.UtcNow - mostRecent.CreatedAt;
                if (timeSince.TotalHours < 1)
                    LastBackupText = $"{(int)timeSince.TotalMinutes} min ago";
                else if (timeSince.TotalDays < 1)
                    LastBackupText = $"{(int)timeSince.TotalHours}h ago";
                else
                    LastBackupText = $"{(int)timeSince.TotalDays}d ago";
            }
            else
            {
                LastBackupText = "Never";
            }

            // Populate save states collection
            SaveStates.Clear();
            foreach (var saveState in saveStates.OrderByDescending(s => s.CreatedAt))
            {
                var timeSince = DateTime.UtcNow - saveState.CreatedAt;
                string createdText;
                if (timeSince.TotalDays < 1)
                    createdText = $"Today {saveState.CreatedAt:HH:mm}";
                else if (timeSince.TotalDays < 7)
                    createdText = $"{timeSince.Days} day{(timeSince.Days >= 2 ? "s" : "")} ago";
                else
                    createdText = saveState.CreatedAt.ToString("MMM d, yyyy");

                var sizeMB = saveState.FileSizeBytes / (1024.0 * 1024.0);
                var fileSizeText = sizeMB < 1024 ? $"{sizeMB:F1} MB" : $"{sizeMB / 1024.0:F2} GB";

                var vm = new GameSaveStateViewModel
                {
                    Id = saveState.Id,
                    DisplayName = saveState.Description ?? $"Save {saveState.CreatedAt:yyyy-MM-dd HH:mm}",
                    Description = saveState.Description ?? string.Empty,
                    CreatedText = createdText,
                    FileSizeText = fileSizeText,
                    BranchName = "main", // TODO: Implement branch support in SaveState entity
                    BranchColor = "#4CAF50",
                    IsCurrentSave = false, // TODO: Implement current save tracking
                    LoadAction = () => PerformLoad(saveState.Id),
                    DeleteAction = () => PerformDelete(saveState.Id)
                };
                SaveStates.Add(vm);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load save states for game {GameId}", gameId);
        }
    }

    private async void PerformLoad(Guid saveStateId)
    {
        try
        {
            if (await _dialogService.ShowConfirmationAsync("Load Save State", "Are you sure you want to load this save state? Unsaved progress will be lost."))
            {
                var command = new RestoreSaveStateCommand(saveStateId);
                var result = await _mediator.Send(command);
                if (result.IsSuccess)
                {
                    await _dialogService.ShowInformationAsync("Success", "Save state loaded successfully.");
                }
                else
                {
                    await _dialogService.ShowErrorAsync("Error", "Failed to load save state.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load save state {SaveStateId}", saveStateId);
            await _dialogService.ShowErrorAsync("Error", $"An error occurred while loading the save state: {ex.Message}");
        }
    }

    private async void PerformDelete(Guid saveStateId)
    {
        try
        {
            if (await _dialogService.ShowConfirmationAsync("Delete Save State", "Are you sure you want to delete this save state?"))
            {
                var command = new DeleteSaveStateCommand(saveStateId);
                var result = await _mediator.Send(command);
                if (result.IsSuccess && _currentGameId != null)
                {
                    await LoadDataAsync(_currentGameId);
                }
                else
                {
                    await _dialogService.ShowErrorAsync("Error", "Failed to delete save state.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete save state {SaveStateId}", saveStateId);
            await _dialogService.ShowErrorAsync("Error", $"An error occurred while deleting the save state: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ConfigureAutoSave()
    {
        // TODO: Open auto-save configuration dialog
        _logger.LogInformation("Configure auto-save requested");
    }

    [RelayCommand]
    private async Task CreateBranch()
    {
        // TODO: Create new branch from current save
        await _dialogService.ShowInformationAsync("Coming Soon", "Branch management will be available in a future update.");
    }

    [RelayCommand]
    private async Task MergeBranch()
    {
        await _dialogService.ShowInformationAsync("Coming Soon", "Branch merging will be available in a future update.");
    }

    [RelayCommand]
    private async Task CompareBranches()
    {
        await _dialogService.ShowInformationAsync("Coming Soon", "Branch comparison will be available in a future update.");
    }

    [RelayCommand]
    private async Task SwitchBranch()
    {
        await _dialogService.ShowInformationAsync("Coming Soon", "Branch switching will be available in a future update.");
    }

    [RelayCommand]
    private async Task CreateManualSave()
    {
        if (_currentGameId == null) return;

        var input = await _dialogService.ShowNoteEditorAsync(null, null); // Using NoteEditor for text input
        if (input != null)
        {
             var description = !string.IsNullOrWhiteSpace(input.Content) ? $"{input.Title} - {input.Content}" : input.Title;
             var command = new CreateSaveStateCommand(_currentGameId.Value, description);
             var result = await _mediator.Send(command);

             if (result.IsSuccess)
             {
                 await LoadDataAsync(_currentGameId);
             }
             else
             {
                 await _dialogService.ShowErrorAsync("Error", "Failed to create save state.");
             }
        }
    }

    [RelayCommand]
    private async Task BackupAll()
    {
        if (_currentGameId == null) return;

        try
        {
            var command = new CreateBackupCommand
            {
                Type = Core.Common.Enums.BackupType.Full,
                Name = $"Backup for Game {_currentGameId}",
                GameIds = new[] { _currentGameId! },
                IncludeSettings = false
            };

            await _dialogService.ShowInformationAsync("Backup Started", "Starting backup process...");

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                await _dialogService.ShowInformationAsync("Success", "Backup completed successfully.");
                await LoadDataAsync(_currentGameId!); // Reload to update Last Backup text
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Backup failed: {result.Error}");
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to create backup for game {GameId}", _currentGameId);
             await _dialogService.ShowErrorAsync("Error", "An unexpected error occurred during backup.");
        }
    }
}

/// <summary>
/// View model for individual save states.
/// </summary>
public partial class GameSaveStateViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public Action? LoadAction { get; set; }
    public Action? DeleteAction { get; set; }

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _createdText = string.Empty;

    [ObservableProperty]
    private string _fileSizeText = string.Empty;

    [ObservableProperty]
    private string _branchName = string.Empty;

    [ObservableProperty]
    private string _branchColor = "#4CAF50";

    [ObservableProperty]
    private string _backgroundBrush = "Transparent";

    [ObservableProperty]
    private string _borderBrush = "Transparent";

    [ObservableProperty]
    private float _opacity = 1.0f;

    [ObservableProperty]
    private bool _isCurrentSave;

    public string StatusColor => IsCurrentSave ? "#4CAF50" : "#666666";
    public string StatusText => IsCurrentSave ? "Current" : "Available";
    public string PrimaryActionText => IsCurrentSave ? "Load" : "Load";
    public string PrimaryActionClass => "Secondary";

    [RelayCommand]
    private void Load()
    {
        LoadAction?.Invoke();
    }

    [RelayCommand]
    private void SaveAs()
    {
        // TODO: Create new save from this state
    }

    [RelayCommand]
    private void CreateBranchFrom()
    {
        // TODO: Create branch from this save
    }

    [RelayCommand]
    private void CopyToBranch()
    {
        // TODO: Copy to different branch
    }

    [RelayCommand]
    private void Settings()
    {
        // TODO: Open save settings
    }

    [RelayCommand]
    private void Delete()
    {
        DeleteAction?.Invoke();
    }
}
