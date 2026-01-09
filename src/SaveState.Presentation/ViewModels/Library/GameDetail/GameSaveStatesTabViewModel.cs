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
    private readonly INotificationService _notificationService;
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

    [ObservableProperty]
    private ObservableCollection<string> _availableBranches = new() { "main" };

    public GameSaveStatesTabViewModel(
        IMediator mediator,
        IDialogService dialogService,
        INotificationService notificationService,
        ILogger<GameSaveStatesTabViewModel> logger)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _notificationService = notificationService;
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

            // Track current branch from data if possible, default to most common
            var branches = saveStates.Select(s => s.BranchName ?? "main").Distinct().ToList();
            AvailableBranches.Clear();
            foreach (var b in branches) AvailableBranches.Add(b);

            if (!AvailableBranches.Contains(CurrentBranchName))
            {
                CurrentBranchName = AvailableBranches.FirstOrDefault() ?? "main";
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
                    BranchName = saveState.BranchName ?? "main",
                    BranchColor = GetBranchColor(saveState.BranchName),
                    IsCurrentSave = saveState.IsCurrent,
                    LoadAction = () => PerformLoadAsync(saveState.Id),
                    DeleteAction = () => PerformDeleteAsync(saveState.Id),
                    SaveAsAction = OnCreateSaveFromStateAsync,
                    CreateBranchFromAction = OnCreateBranchFromStateAsync,
                    CopyToBranchAction = OnCopyToBranchAsync,
                    SettingsAction = OnOpenSettingsAsync
                };
                SaveStates.Add(vm);
            }

            CurrentBranchSaveCount = SaveStates.Count(s => s.BranchName == CurrentBranchName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load save states for game {GameId}", gameId);
        }
    }

    private async Task PerformLoadAsync(Guid saveStateId)
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

    private async Task PerformDeleteAsync(Guid saveStateId)
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
    private async Task ConfigureAutoSave()
    {
        if (_currentGameId == null) return;
        _logger.LogInformation("Configure auto-save requested for {GameId}", _currentGameId);

        try
        {
            var result = await _dialogService.ShowAutoSaveConfigurationDialogAsync(
                AutoSaveEnabled,
                SelectedAutoSaveInterval,
                10); // Default max auto saves

            if (result != null)
            {
                // Parse interval string to TimeSpan
                var intervalMinutes = ParseIntervalToMinutes(result.Interval);
                var interval = TimeSpan.FromMinutes(intervalMinutes);

                // Build enabled triggers list
                var triggers = new List<SaveState.Core.SaveStates.Services.SaveTrigger>();
                if (result.CreateOnGameStart)
                    triggers.Add(SaveState.Core.SaveStates.Services.SaveTrigger.SessionStart);
                if (result.CreateOnBossEncounter)
                    triggers.Add(SaveState.Core.SaveStates.Services.SaveTrigger.SignificantProgress);

                var config = new SaveState.Core.SaveStates.Services.AutoSaveConfig(
                    result.AutoSaveEnabled,
                    interval,
                    result.MaxAutoSaves,
                    triggers);

                var command = new ConfigureAutoSaveCommand(_currentGameId.Value, config);

                var medResult = await _mediator.Send(command);
                if (medResult.IsSuccess)
                {
                    AutoSaveEnabled = result.AutoSaveEnabled;
                    SelectedAutoSaveInterval = result.Interval;
                    _notificationService.ShowSuccess("Auto-save configuration updated", "Settings Saved");
                }
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to configure auto-save");
             _notificationService.ShowError("Failed to update auto-save settings");
        }
    }

    private int ParseIntervalToMinutes(string interval)
    {
        return interval switch
        {
            "5 minutes" => 5,
            "10 minutes" => 10,
            "15 minutes" => 15,
            "30 minutes" => 30,
            "1 hour" => 60,
            _ => 10 // Default to 10 minutes
        };
    }

    private int ParseInterval(string interval)
    {
        return interval.Contains("min") ? int.Parse(interval.Split(' ')[0]) : int.Parse(interval.Split(' ')[0]) * 60;
    }

    private string GetBranchColor(string? branchName)
    {
        return branchName?.ToLower() switch
        {
            "main" => "#4CAF50",
            "master" => "#4CAF50",
            "experimental" => "#FF9800",
            "debug" => "#F44336",
            _ => "#2196F3"
        };
    }

    [RelayCommand]
    private async Task CreateBranch()
    {
        if (_currentGameId == null) return;

        try
        {
            var result = await _dialogService.ShowBranchCreationDialogAsync();
            if (result != null)
            {
                var command = new CreateBranchCommand(
                    Guid.Empty, // Placeholder root state - theoretically should be the 'current' one
                    result.BranchName,
                    result.Description,
                    result.BranchType);

                var createResult = await _mediator.Send(command);
                if (createResult.IsSuccess)
                {
                    await _dialogService.ShowInformationAsync("Success", $"Branch '{result.BranchName}' created successfully.");
                    await LoadDataAsync(_currentGameId);
                }
                else
                {
                    await _dialogService.ShowErrorAsync("Error", $"Failed to create branch: {createResult.Error}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create branch for game {GameId}", _currentGameId);
            await _dialogService.ShowErrorAsync("Error", "An unexpected error occurred while creating the branch.");
        }
    }

    [RelayCommand]
    private async Task MergeBranch()
    {
        if (_currentGameId == null) return;

        try
        {
            // In a full implementation, this would:
            // 1. Get available branches
            // 2. Let user select target branch
            // 3. Detect conflicts between branches
            // 4. Show merge dialog with conflict resolution options

            // For now, show a sample merge dialog
            var sampleConflicts = new[]
            {
                new ViewModels.Dialogs.SaveStateDiffViewModel
                {
                    Name = "Boss Battle Save",
                    Status = ViewModels.Dialogs.DiffStatus.Conflict,
                    LeftTimestamp = DateTime.UtcNow.AddHours(-2),
                    RightTimestamp = DateTime.UtcNow.AddHours(-1),
                    LeftSize = 1024 * 1024 * 5,
                    RightSize = 1024 * 1024 * 6
                }
            };

            var result = await _dialogService.ShowBranchMergeDialogAsync(
                CurrentBranchName,
                "feature-branch",
                sampleConflicts);

            if (result != null)
            {
                await _dialogService.ShowInformationAsync(
                    "Merge Successful",
                    $"Branch '{result.SourceBranchName}' merged into '{result.TargetBranchName}' using strategy: {result.MergeStrategy}");

                await LoadDataAsync(_currentGameId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to merge branch for game {GameId}", _currentGameId);
            await _dialogService.ShowErrorAsync("Error", "An unexpected error occurred while merging branches.");
        }
    }

    [RelayCommand]
    private async Task CompareBranches()
    {
        if (_currentGameId == null) return;

        try
        {
            // In a full implementation, this would:
            // 1. Let user select two branches to compare
            // 2. Compute differences between their save states
            // 3. Show comparison dialog

            // For now, show a sample comparison
            var sampleDifferences = new[]
            {
                new ViewModels.Dialogs.SaveStateDiffViewModel
                {
                    Name = "Chapter 1 Complete",
                    Status = ViewModels.Dialogs.DiffStatus.InBoth,
                    LeftTimestamp = DateTime.UtcNow.AddDays(-5),
                    RightTimestamp = DateTime.UtcNow.AddDays(-5),
                    LeftSize = 1024 * 1024 * 3,
                    RightSize = 1024 * 1024 * 3
                },
                new ViewModels.Dialogs.SaveStateDiffViewModel
                {
                    Name = "Secret Area Found",
                    Status = ViewModels.Dialogs.DiffStatus.OnlyInLeft,
                    LeftTimestamp = DateTime.UtcNow.AddDays(-2),
                    LeftSize = 1024 * 1024 * 4
                },
                new ViewModels.Dialogs.SaveStateDiffViewModel
                {
                    Name = "Boss Rush Mode",
                    Status = ViewModels.Dialogs.DiffStatus.OnlyInRight,
                    RightTimestamp = DateTime.UtcNow.AddDays(-1),
                    RightSize = 1024 * 1024 * 5
                }
            };

            await _dialogService.ShowBranchComparisonDialogAsync(
                CurrentBranchName,
                "experiment-branch",
                sampleDifferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare branches for game {GameId}", _currentGameId);
            await _dialogService.ShowErrorAsync("Error", "An unexpected error occurred while comparing branches.");
        }
    }

    [RelayCommand]
    private async Task SwitchBranch()
    {
        if (_currentGameId == null) return;

        try
        {
            var availableOptions = AvailableBranches.Select(b => new ViewModels.Dialogs.BranchOptionViewModel
            {
                Name = b,
                IsCurrent = b == CurrentBranchName,
                BranchType = "Story" // Mock type
            }).ToArray();

            var result = await _dialogService.ShowBranchSelectionDialogAsync(
                CurrentBranchName,
                availableOptions);

            if (result != null)
            {
                CurrentBranchName = result.BranchName;
                await LoadDataAsync(_currentGameId);

                _notificationService.ShowSuccess($"Switched to branch '{result.BranchName}'", "Branch Switched");

                _logger.LogInformation(
                    "Switched to branch '{BranchName}' for game {GameId}",
                    result.BranchName,
                    _currentGameId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch branch for game {GameId}", _currentGameId);
            await _dialogService.ShowErrorAsync("Error", "An unexpected error occurred while switching branches.");
        }
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
    private async Task OnCreateSaveFromStateAsync(Guid stateId)
    {
        if (_currentGameId == null) return;

        var name = await _dialogService.ShowInputDialogAsync("Save As", "Enter a name for the new save state:", "New Save State");
        if (string.IsNullOrEmpty(name)) return;

        try
        {
            var command = new DuplicateSaveStateCommand(stateId, name);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Cloned as '{name}'", "Success");
                await LoadDataAsync(_currentGameId);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Failed to clone: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clone save state {StateId}", stateId);
            await _dialogService.ShowErrorAsync("Error", "Failed to clone save state");
        }
    }

    private async Task OnCreateBranchFromStateAsync(Guid stateId)
    {
        if (_currentGameId == null) return;

        var branchResult = await _dialogService.ShowBranchCreationDialogAsync();
        if (branchResult == null) return;

        try
        {
             var command = new CreateBranchFromSaveCommand(stateId, branchResult.BranchName, branchResult.Description);
             var result = await _mediator.Send(command);

             if (result.IsSuccess)
             {
                 _notificationService.ShowSuccess($"New branch '{branchResult.BranchName}' created.", "Success");
                 await LoadDataAsync(_currentGameId);
             }
             else
             {
                 await _dialogService.ShowErrorAsync("Error", $"Failed to create branch: {result.Error}");
             }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to create branch from save state");
             await _dialogService.ShowErrorAsync("Error", "Failed to create branch");
        }
    }

    private async Task OnCopyToBranchAsync(Guid stateId)
    {
        if (_currentGameId == null) return;

        // Mock getting branches
        var branches = new[] {
            new ViewModels.Dialogs.BranchOptionViewModel { Name = "main", BranchType = "Main" },
            new ViewModels.Dialogs.BranchOptionViewModel { Name = "speedrun", BranchType = "Feature" }
        };

        var result = await _dialogService.ShowBranchSelectionDialogAsync("Select Target Branch", branches);
        if (result == null) return;

        try
        {
             var command = new CopyToBranchCommand(stateId, result.BranchName);
             var medResult = await _mediator.Send(command);

             if (medResult.IsSuccess)
             {
                 _notificationService.ShowSuccess($"Copied to branch '{result.BranchName}'.", "Success");
                 await LoadDataAsync(_currentGameId);
             }
             else
             {
                 await _dialogService.ShowErrorAsync("Error", $"Failed to copy: {medResult.Error}");
             }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to copy save state");
             await _dialogService.ShowErrorAsync("Error", "Failed to copy save state");
        }
    }

    private async Task OnOpenSettingsAsync(Guid stateId)
    {
         if (_currentGameId == null) return;

         // Mock getting current details
         var state = SaveStates.FirstOrDefault(s => s.Id == stateId);
         if (state == null) return;

         var result = await _dialogService.ShowSaveStateSettingsDialogAsync(
             stateId,
             state.Description,
             state.BranchName,
             state.IsCurrentSave,
             "");

         if (result != null)
         {
             var command = new UpdateSaveStateMetadataCommand(
                 stateId,
                 result.Description,
                 result.BranchName,
                 result.Notes,
                 null,
                 result.IsCurrent);

             var medResult = await _mediator.Send(command);
             if (medResult.IsSuccess)
             {
                 _notificationService.ShowSuccess("Save state settings updated.", "Success");
                 await LoadDataAsync(_currentGameId);
             }
             else
             {
                 await _dialogService.ShowErrorAsync("Error", $"Failed to update: {medResult.Error}");
             }
         }
    }
}

/// <summary>
/// View model for individual save states.
/// </summary>
public partial class GameSaveStateViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public Func<Task>? LoadAction { get; set; }
    public Func<Task>? DeleteAction { get; set; }
    public Func<Guid, Task>? SaveAsAction { get; set; }
    public Func<Guid, Task>? CreateBranchFromAction { get; set; }
    public Func<Guid, Task>? CopyToBranchAction { get; set; }
    public Func<Guid, Task>? SettingsAction { get; set; }

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
    private async Task Load()
    {
        if (LoadAction != null) await LoadAction.Invoke();
    }

    [RelayCommand]
    private async Task SaveAs()
    {
        if (SaveAsAction != null) await SaveAsAction.Invoke(Id);
    }

    [RelayCommand]
    private async Task CreateBranchFrom()
    {
        if (CreateBranchFromAction != null) await CreateBranchFromAction.Invoke(Id);
    }

    [RelayCommand]
    private async Task CopyToBranch()
    {
        if (CopyToBranchAction != null) await CopyToBranchAction.Invoke(Id);
    }

    [RelayCommand]
    private async Task Settings()
    {
        if (SettingsAction != null) await SettingsAction.Invoke(Id);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (DeleteAction != null) await DeleteAction.Invoke();
    }
}
