using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.TournamentManagement.Models;
using SaveState.Presentation.ViewModels.Automation;
using SaveState.Presentation.ViewModels.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Service for showing dialogs and overlays.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a note editor dialog.
    /// </summary>
    Task<NoteEditorResult?> ShowNoteEditorAsync(
        Guid? noteId = null,
        string? initialContent = null,
        string? title = null,
        string? category = null,
        bool isPinned = false);

    /// <summary>
    /// Shows a simple input dialog.
    /// </summary>
    Task<string?> ShowInputDialogAsync(
        string title,
        string message,
        string? placeholder = null,
        bool isSensitive = false);

    /// <summary>
    /// Shows a tag editor dialog.
    /// </summary>
    Task<TagEditorResult?> ShowTagEditorAsync(string[] currentTags);

    /// <summary>
    /// Shows a goal creation dialog.
    /// </summary>
    Task<GoalCreationResult?> ShowGoalCreationDialogAsync();

    /// <summary>
    /// Shows a review editor dialog.
    /// </summary>
    Task<ReviewEditorResult?> ShowReviewEditorAsync(string? existingReview = null, int? existingRating = null);

    /// <summary>
    /// Shows a file picker dialog for selecting mod files.
    /// </summary>
    Task<string[]?> ShowModFilePickerAsync();

    /// <summary>
    /// Shows a generic file picker dialog.
    /// </summary>
    Task<string?> ShowFilePickerAsync(string title, string[] extensions);

    /// <summary>
    /// Shows a file open dialog.
    /// </summary>
    Task<string?> ShowOpenFileDialogAsync(string title, string[] extensions);

    /// <summary>
    /// Shows a file open dialog with filter name and patterns.
    /// </summary>
    Task<string?> ShowOpenFileDialogAsync(string title, string filterName, string[] filterPatterns);

    /// <summary>
    /// Shows a file save dialog.
    /// </summary>
    Task<string?> ShowSaveFileDialogAsync(string title, string[] extensions, string? defaultFileName = null);

    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel");

    /// <summary>
    /// Shows an information dialog.
    /// </summary>
    Task ShowInformationAsync(string title, string message);

    /// <summary>
    /// Shows an error dialog.
    /// </summary>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows a simple error dialog with just a message.
    /// </summary>
    Task ShowErrorAsync(string message);

    /// <summary>
    /// Shows a success dialog.
    /// </summary>
    Task ShowSuccessAsync(string message);

    /// <summary>
    /// Shows a warning dialog.
    /// </summary>
    Task ShowWarningAsync(string title, string message);

    /// <summary>
    /// Shows a message dialog with optional icon.
    /// </summary>
    Task ShowMessageDialogAsync(string title, string message, string? icon = null);

    /// <summary>
    /// Shows a dialog that lets the user select a collection to move games into.
    /// </summary>
    Task<CollectionSelectionResult?> ShowCollectionSelectionDialogAsync(
        IReadOnlyList<CollectionSelectionOption> collections,
        Guid? currentSelectionId = null);

    /// <summary>
    /// Shows the task creation dialog.
    /// </summary>
    Task<TaskCreationResult?> ShowTaskCreationDialogAsync(ScheduledTaskViewModel? existingTask = null);

    /// <summary>
    /// Shows the workflow creation dialog.
    /// </summary>
    Task<WorkflowCreationResult?> ShowWorkflowCreationDialogAsync(WorkflowViewModel? existingWorkflow = null);

    /// <summary>
    /// Shows the macro recorder dialog.
    /// </summary>
    Task<MacroViewModel?> ShowMacroRecorderDialogAsync();

    /// <summary>
    /// Shows the automation settings dialog.
    /// </summary>
    Task ShowAutomationSettingsDialogAsync();

    /// <summary>
    /// Shows the multi-step add game wizard.
    /// </summary>
    Task<AddGameResult?> ShowAddGameWizardAsync();

    /// <summary>
    /// Shows the emulator editor dialog.
    /// </summary>
    Task<EmulatorEditorResult?> ShowEmulatorEditorAsync(SaveState.Core.RomManagement.Entities.Emulator? existingEmulator = null);

    /// <summary>
    /// Shows the ROM details dialog.
    /// </summary>
    Task ShowRomDetailsDialogAsync(SaveState.Core.RomManagement.Entities.RomFile romFile);

    /// <summary>
    /// Shows the emulator configuration dialog.
    /// </summary>
    Task<EmulatorConfigResult?> ShowEmulatorConfigDialogAsync(SaveState.Core.RomManagement.Entities.Emulator? existingEmulator = null);

    /// <summary>
    /// Shows the ROM scan progress dialog with the specified scan action.
    /// </summary>
    /// <param name="scanAction">The scan action to execute with cancellation support.</param>
    Task ShowRomScanProgressDialogAsync(Func<CancellationToken, Task> scanAction);

    /// <summary>
    /// Shows the ROM metadata editing dialog.
    /// </summary>
    Task<RomMetadataResult?> ShowRomMetadataDialogAsync(string title, string? description, string? region, string? version);

/// <summary>
/// Result from the emulator configuration dialog.
/// </summary>
public record EmulatorConfigResult(
    Guid EmulatorId,
    string Name,
    string ExecutablePath,
    Guid PlatformId,
    string? Version,
    string? Description,
    string? CommandLineArgs,
    bool IsAvailable);

    /// <summary>
    /// Shows a folder picker dialog.
    /// </summary>
    Task<string?> ShowFolderPickerAsync(string? title = null, string? initialPath = null);

    /// <summary>
    /// Shows the branch creation dialog.
    /// </summary>
    Task<BranchCreationResult?> ShowBranchCreationDialogAsync();

    /// <summary>
    /// Shows the save state settings dialog.
    /// </summary>
    Task<SaveStateSettingsResult?> ShowSaveStateSettingsDialogAsync(
        Guid saveStateId,
        string? description = null,
        string? branchName = null,
        bool isCurrent = false,
        string? notes = null);

    /// <summary>
    /// Shows the branch selection dialog for switching branches.
    /// </summary>
    Task<BranchSelectionResult?> ShowBranchSelectionDialogAsync(
        string currentBranchName,
        ViewModels.Dialogs.BranchOptionViewModel[] availableBranches);

    /// <summary>
    /// Shows the branch comparison dialog.
    /// </summary>
    Task ShowBranchComparisonDialogAsync(
        string leftBranchName,
        string rightBranchName,
        ViewModels.Dialogs.SaveStateDiffViewModel[] differences);

    /// <summary>
    /// Shows the branch merge dialog.
    /// </summary>
    Task<BranchMergeResult?> ShowBranchMergeDialogAsync(
        string sourceBranchName,
        string targetBranchName,
        ViewModels.Dialogs.SaveStateDiffViewModel[] conflicts);

    /// <summary>
    /// Shows the launch configuration dialog.
    /// </summary>
    Task<LaunchConfigResult?> ShowLaunchConfigDialogAsync(
        Guid gameId,
        string? currentArguments = null);

    /// <summary>
    /// Shows the game rating dialog.
    /// </summary>
    Task<GameRatingResult?> ShowGameRatingDialogAsync(
        Guid gameId,
        double? currentRating = null);

    /// <summary>
    /// Shows the cloud provider configuration dialog.
    /// </summary>
    Task<CloudProviderConfigResult?> ShowCloudProviderConfigDialogAsync(
        CloudProviderConfigResult? currentSettings = null);

    /// <summary>
    /// Shows the sync conflict resolution dialog.
    /// </summary>
    Task<ConflictResolutionResult?> ShowConflictResolutionDialogAsync(
        SyncConflictViewModel[] conflicts);

    /// <summary>
    /// Shows the workflow editor dialog for visual workflow building.
    /// </summary>
    Task<WorkflowEditorResult?> ShowWorkflowEditorDialogAsync(
        Guid? workflowId = null);

    /// <summary>
    /// Shows the macro playback dialog with execution progress.
    /// </summary>
    Task ShowMacroPlaybackDialogAsync(
        Guid macroId,
        string macroName);

    /// <summary>
    /// Shows the auto-save configuration dialog.
    /// </summary>
    Task<AutoSaveConfigurationResult?> ShowAutoSaveConfigurationDialogAsync(
        bool autoSaveEnabled = true,
        string selectedInterval = "5 min",
        int maxAutoSaves = 10);

    /// <summary>
    /// Shows the price alert dialog.
    /// </summary>
    Task<PriceAlertResult?> ShowPriceAlertDialogAsync(
        string gameTitle,
        double currentPrice);

    /// <summary>
    /// Shows the price history chart for a game.
    /// </summary>
    Task ShowPriceHistoryChartAsync(string gameTitle);

    /// <summary>
    /// Shows the emulator setup wizard.
    /// </summary>
    Task ShowEmulatorSetupWizardAsync();

    /// <summary>
    /// Shows the game executable configuration dialog.
    /// </summary>
    Task<GameExecutableConfigResult?> ShowGameExecutableConfigAsync(
        Guid gameId,
        string gameTitle,
        string? currentExecutablePath = null,
        string? currentLaunchArguments = null);

    /// <summary>
    /// Shows the launch profile editor dialog.
    /// </summary>
    Task<LaunchProfileResult?> ShowLaunchProfileEditorAsync(
        SaveState.Core.SmartLauncher.LaunchProfile? existingProfile = null);

    /// <summary>
    /// Shows the process selector dialog for attaching to a running game process.
    /// </summary>
    /// <returns>The selected process ID, or null if cancelled.</returns>
    Task<int?> ShowProcessSelectorAsync();

    /// <summary>
    /// Shows the Cheat Engine table import dialog.
    /// </summary>
    /// <returns>Import result containing imported signatures and statistics.</returns>
    Task<ImportCheatTableResult?> ShowImportCheatTableDialogAsync();

    /// <summary>
    /// Shows the error log viewer dialog.
    /// </summary>
    Task ShowErrorLogViewerAsync();

    /// <summary>
    /// Shows a generic dialog with the specified ViewModel.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the dialog.</typeparam>
    /// <param name="viewModel">The ViewModel for the dialog.</param>
    /// <returns>The result from the dialog, or null if cancelled.</returns>
    Task<TResult?> ShowDialogAsync<TResult>(object viewModel);

    /// <summary>
    /// Shows the launch experience configuration dialog.
    /// </summary>
    Task<LaunchExperienceConfigResult?> ShowLaunchExperienceConfigAsync();

    /// <summary>
    /// Shows the game performance detail dialog.
    /// </summary>
    Task ShowGamePerformanceDetailAsync(ViewModels.Settings.GamePerformanceStats gameStats);

    /// <summary>
    /// Shows a simple message dialog.
    /// </summary>
    Task ShowMessageAsync(string title, string message);

    /// <summary>
    /// Shows the import preview dialog with conflict resolution.
    /// </summary>
    /// <param name="preview">The import preview data.</param>
    /// <param name="filePath">Optional path to the import file.</param>
    /// <returns>The import preview result with user selections, or null if cancelled.</returns>
    Task<ImportPreviewResult?> ShowImportPreviewAsync(Models.Data.ImportPreview preview, string? filePath = null);

    /// <summary>
    /// Shows the create tournament dialog.
    /// </summary>
    /// <returns>The tournament creation result, or null if cancelled.</returns>
    Task<CreateTournamentResult?> ShowCreateTournamentDialogAsync();

    /// <summary>
    /// Shows the match result dialog.
    /// </summary>
    /// <param name="match">The match to report results for.</param>
    /// <returns>The match result, or null if cancelled.</returns>
    Task<MatchResultDialogResult?> ShowMatchResultDialogAsync(SaveState.Core.TournamentManagement.Models.TournamentMatch match);

    /// <summary>
    /// Shows a text input dialog.
    /// </summary>
    Task<string?> ShowTextInputAsync(string title, string message, string? defaultValue = null);

    /// <summary>
    /// Shows an info dialog.
    /// </summary>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Closes the current dialog with the specified result.
    /// </summary>
    void CloseDialog(object? result = null);
}

/// <summary>
/// Result from the game executable configuration dialog.
/// </summary>
public record GameExecutableConfigResult(
    string ExecutablePath,
    string? LaunchArguments);

/// <summary>
/// Result from the launch profile editor dialog.
/// </summary>
public record LaunchProfileResult(
    string Name,
    string? Description,
    SaveState.Core.SmartLauncher.ProcessPriority Priority,
    List<string> ProcessesToSuspend,
    bool EnableMemoryOptimization,
    bool ClearStandbyList,
    bool DisableVisualEffects);

/// <summary>
/// Result from the auto-save configuration dialog.
/// </summary>
public record AutoSaveConfigurationResult(
    bool AutoSaveEnabled,
    string Interval,
    int MaxAutoSaves,
    bool CreateOnGameStart,
    bool CreateOnBossEncounter,
    bool NotifyOnAutoSave,
    bool CompressAutoSaves);

/// <summary>
/// Result from the price alert dialog.
/// </summary>
public record PriceAlertResult(
    double TargetPrice,
    string Store,
    bool EmailNotification,
    bool InAppNotification);

/// <summary>
/// Result from the emulator editor dialog.
/// </summary>
public record EmulatorEditorResult(
    string Name,
    string ExecutablePath,
    string? Version,
    string? Description,
    bool KeepExisting);

/// <summary>
/// Result from the add game wizard.
/// </summary>
public record AddGameResult(
    string Title,
    string? Path,
    string? Platform,
    bool ScanAutomatically);

/// <summary>
/// Result from the note editor dialog.
/// </summary>
public record NoteEditorResult(
    string Title,
    string Content,
    string Category,
    bool IsPinned);

/// <summary>
/// Result from the tag editor dialog.
/// </summary>
public record TagEditorResult(
    string[] Tags);

/// <summary>
/// Result from the goal creation dialog.
/// </summary>
public record GoalCreationResult(
    string Title,
    string Description,
    DateTime? TargetDate,
    string GoalType);

/// <summary>
/// Result from the review editor dialog.
/// </summary>
public record ReviewEditorResult(
    string ReviewText,
    int Rating,
    bool RecommendToFriends);

public record TaskCreationResult(
    string Name,
    string Schedule,
    bool IsEnabled);

public record WorkflowCreationResult(
    string Name,
    string Description,
    string Icon);

/// <summary>
/// Result from the branch creation dialog.
/// </summary>
public record BranchCreationResult(
    string BranchName,
    string Description,
    SaveState.Core.SaveStates.Entities.BranchType BranchType);

/// <summary>
/// Result from the save state settings dialog.
/// </summary>
public record SaveStateSettingsResult(
    Guid SaveStateId,
    string Description,
    string BranchName,
    bool IsCurrent,
    string Notes,
    string[] Tags);

/// <summary>
/// Result from the branch selection dialog.
/// </summary>
public record BranchSelectionResult(
    string BranchName,
    string BranchType);

/// <summary>
/// Result from the branch merge dialog.
/// </summary>
public record BranchMergeResult(
    string SourceBranchName,
    string TargetBranchName,
    bool KeepBothOnConflict,
    string MergeStrategy);

/// <summary>
/// Result from the launch configuration dialog.
/// </summary>
public record LaunchConfigResult(
    string LaunchArguments,
    bool UseCustomResolution,
    int? Width,
    int? Height,
    bool StartInFullScreen);

/// <summary>
/// Result from the game rating dialog.
/// </summary>
public record GameRatingResult(
    double Rating,
    string? ReviewText);

/// <summary>
/// Result from the cloud provider configuration dialog.
/// </summary>
public record CloudProviderConfigResult(
    string ProviderName,
    string ApiKey,
    string? BucketName,
    bool EnableAutoSync,
    bool EnableBackgroundFailureAlerts,
    bool EnableBackgroundConflictAlerts,
    int AlertCooldownSeconds);

/// <summary>
/// Result from the conflict resolution dialog.
/// </summary>
public record ConflictResolutionResult(
    Dictionary<string, string> Resolutions); // filepath -> resolution strategy

/// <summary>
/// View model for a sync conflict.
/// </summary>
public record SyncConflictViewModel(
    string FilePath,
    DateTime LocalModified,
    DateTime CloudModified,
    long LocalSize,
    long CloudSize);


/// <summary>
/// Result from the workflow editor dialog.
/// </summary>
public record WorkflowEditorResult(
    Guid WorkflowId,
    string Name,
    string Description,
    List<WorkflowStepViewModel> Steps);

/// <summary>
/// Result from the ROM metadata dialog.
/// </summary>
public record RomMetadataResult(
    string Title,
    string? Description,
    string? Region,
    string? Version);

/// <summary>
/// View model for a workflow step.
/// </summary>
public record WorkflowStepViewModel(
    string StepType,
    string Name,
    Dictionary<string, string> Parameters,
    int Order);

/// <summary>
/// Result from the Cheat Engine table import dialog.
/// </summary>
public record ImportCheatTableResult(
    List<GameMemorySignature> ImportedSignatures,
    int SuccessfullyImported,
    int Skipped,
    int Failed,
    List<string> ErrorMessages);

/// <summary>
/// Result from the launch experience configuration dialog.
/// </summary>
public record LaunchExperienceConfigResult(
    bool EnableCinematicLaunch,
    bool ShowGameFacts,
    bool ShowLastProgress,
    bool ShowAchievementProgress,
    int DurationSeconds);

/// <summary>
/// Result from the create tournament dialog.
/// </summary>
public record CreateTournamentResult(
    string Name,
    string Description,
    string GameId,
    SaveState.Core.TournamentManagement.Models.TournamentFormat Format,
    DateTime RegistrationStart,
    DateTime RegistrationEnd,
    DateTime TournamentStart,
    int MaxParticipants,
    SaveState.Core.TournamentManagement.Models.TournamentRules Rules,
    SaveState.Core.TournamentManagement.Models.PrizePool? PrizePool,
    string? StreamUrl);

/// <summary>
/// Result from the match result dialog.
/// </summary>
public record MatchResultDialogResult(
    string MatchId,
    int Player1Score,
    int Player2Score,
    string WinnerId,
    IReadOnlyList<GameResult> GameResults,
    string? ReplayPath,
    string Notes);
