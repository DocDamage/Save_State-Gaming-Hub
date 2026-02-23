using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.TournamentManagement.Models;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.Views.Dialogs;
using System;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services;

/// <summary>
/// Tournament related dialogs for the dialog service.
/// </summary>
public partial class DialogService : IDialogService
{
    /// <summary>
    /// Shows the create tournament dialog.
    /// </summary>
    public async Task<CreateTournamentResult?> ShowCreateTournamentDialogAsync()
    {
        try
        {
            var dialog = new CreateTournamentDialog
            {
                DataContext = new CreateTournamentDialogViewModel(_timeProvider)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for create tournament dialog");
                return null;
            }

            return await dialog.ShowDialog<CreateTournamentResult?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show create tournament dialog");
            return null;
        }
    }

    /// <summary>
    /// Shows the match result dialog.
    /// </summary>
    public async Task<MatchResultDialogResult?> ShowMatchResultDialogAsync(TournamentMatch match)
    {
        try
        {
            var dialog = new MatchResultDialog
            {
                DataContext = new MatchResultDialogViewModel(match)
            };

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for match result dialog");
                return null;
            }

            return await dialog.ShowDialog<MatchResultDialogResult?>(mainWindow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show match result dialog");
            return null;
        }
    }

    /// <summary>
    /// Shows a text input dialog.
    /// </summary>
    public async Task<string?> ShowTextInputAsync(string title, string message, string? defaultValue = null)
    {
        return await ShowInputDialogAsync(title, message, defaultValue);
    }

    /// <summary>
    /// Shows an info dialog.
    /// </summary>
    public async Task ShowInfoAsync(string title, string message)
    {
        await ShowInformationAsync(title, message);
    }
}
