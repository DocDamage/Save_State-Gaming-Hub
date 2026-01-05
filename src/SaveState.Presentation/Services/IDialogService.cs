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
    Task<NoteEditorResult?> ShowNoteEditorAsync(Guid? noteId = null, string? initialContent = null);

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
}

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

