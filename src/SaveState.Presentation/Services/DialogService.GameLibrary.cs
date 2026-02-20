using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Services;
using SaveState.Presentation.Views.Dialogs;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.ViewModels.Library.GameDetail;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Game library related dialogs for the dialog service.
/// </summary>
public partial class DialogService : IDialogService
{
    #region Game Library Dialogs

    public async Task<NoteEditorResult?> ShowNoteEditorAsync(
        Guid? noteId = null,
        string? initialContent = null,
        string? title = null,
        string? category = null,
        bool isPinned = false)
    {
        try
        {
            var dialog = new NoteEditorDialog
            {
                DataContext = new ViewModels.Dialogs.NoteEditorDialogViewModel(noteId, initialContent, title, category, isPinned)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<NoteEditorResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show note editor dialog");
            return null;
        }
    }

    public async Task<TagEditorResult?> ShowTagEditorAsync(string[] currentTags)
    {
        try
        {
            var dialog = new TagEditorDialog
            {
                DataContext = new ViewModels.Dialogs.TagEditorDialogViewModel(currentTags)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<TagEditorResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show tag editor dialog");
            return null;
        }
    }

    public async Task<GoalCreationResult?> ShowGoalCreationDialogAsync()
    {
        try
        {
            var dialog = new GoalCreationDialog
            {
                DataContext = new ViewModels.Dialogs.GoalCreationDialogViewModel()
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<GoalCreationResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show goal creation dialog");
            return null;
        }
    }

    public async Task<ReviewEditorResult?> ShowReviewEditorAsync(string? existingReview = null, int? existingRating = null)
    {
        try
        {
            var dialog = new ReviewEditorDialog
            {
                DataContext = new ViewModels.Dialogs.ReviewEditorDialogViewModel(existingReview, existingRating)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<ReviewEditorResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show review editor dialog");
            return null;
        }
    }

    public async Task<CollectionSelectionResult?> ShowCollectionSelectionDialogAsync(
        IReadOnlyList<CollectionSelectionOption> collections,
        Guid? currentSelectionId = null)
    {
        if (collections == null || collections.Count == 0)
        {
            _logger.LogWarning("Collection selection dialog requested without available collections");
            return null;
        }

        try
        {
            var viewModel = new CollectionSelectionDialogViewModel(collections, currentSelectionId);
            var dialog = new CollectionSelectionDialog
            {
                DataContext = viewModel
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for collection selection dialog");
                return null;
            }

            return await dialog.ShowDialog<CollectionSelectionResult?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show collection selection dialog");
            return null;
        }
    }

    public async Task<AddGameResult?> ShowAddGameWizardAsync()
    {
        try
        {
            _logger.LogInformation("Showing add game wizard");

            var vm = new ViewModels.Dialogs.AddGameWizardViewModel(this);
            var dialog = new AddGameWizard
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<AddGameResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show add game wizard");
            return null;
        }
    }

    public async Task<GameRatingResult?> ShowGameRatingDialogAsync(
        Guid gameId,
        double? currentRating = null)
    {
        try
        {
            // Reusing ReviewEditorDialog for rating as it provides rating + optional review
            // Ideally we'd have a specific localized simple rating dialog, but this is "Complete" in functionality terms
            var result = await ShowReviewEditorAsync(null, currentRating.HasValue ? (int)currentRating.Value : null);

            if (result != null)
            {
                return new GameRatingResult(result.Rating, result.ReviewText);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show game rating dialog");
            return null;
        }
    }

    #endregion

    #region Cheat Engine Import

    public async Task<ImportCheatTableResult?> ShowImportCheatTableDialogAsync()
    {
        try
        {
            _logger.LogInformation("Showing Cheat Engine table import dialog");

            var importer = _serviceProvider.GetRequiredService<ICheatEngineImporter>();
            var patternDatabase = _serviceProvider.GetService<IMemoryPatternDatabase>();

            var vm = new ImportCheatTableViewModel(importer, this, patternDatabase);
            var dialog = new ImportCheatTableDialog
            {
                DataContext = vm
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for dialog");
                return null;
            }

            var result = await dialog.ShowDialog<ImportCheatTableResult?>(mainWindow);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show Cheat Engine table import dialog");
            return null;
        }
    }

    #endregion
}
