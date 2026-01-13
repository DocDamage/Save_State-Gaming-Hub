using SaveState.Presentation.ViewModels.Automation;
using System;
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
    Task<string?> ShowInputDialogAsync(string title, string message, string? placeholder = null);

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
    /// Shows a warning dialog.
    /// </summary>
    Task ShowWarningAsync(string title, string message);

    /// <summary>
    /// Shows a message dialog with optional icon.
    /// </summary>
    Task ShowMessageDialogAsync(string title, string message, string? icon = null);

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
    Task<EmulatorEditorResult?> ShowEmulatorEditorAsync(SaveState.Presentation.ViewModels.Shell.EmulatorViewModel? existingEmulator = null);

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
        string? currentProvider = null);

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
}

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
    bool EnableAutoSync);

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
