// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.SmartLauncher;
using SaveState.Presentation.Views.Dialogs;

namespace SaveState.Presentation.Services;

/// <summary>
/// Smart Launcher dialog methods for DialogService.
/// </summary>
public partial class DialogService
{
    /// <inheritdoc />
    public async Task<GameExecutableConfigResult?> ShowGameExecutableConfigAsync(
        Guid gameId,
        string gameTitle,
        string? currentExecutablePath = null,
        string? currentLaunchArguments = null)
    {
        try
        {
            var dialog = new GameExecutableConfigDialog();

            // Create a temporary game object for the dialog
            var game = Game.Create(gameTitle);
            typeof(Game).GetProperty("Id")?.SetValue(game, gameId);
            if (currentExecutablePath != null)
            {
                game.SetExecutablePath(currentExecutablePath);
            }
            if (currentLaunchArguments != null)
            {
                game.UpdateLaunchConfiguration(currentLaunchArguments);
            }

            dialog.SetGame(game);

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for game executable config dialog");
                return null;
            }

            var result = await dialog.ShowDialog<bool>(mainWindow);
            if (result)
            {
                return new GameExecutableConfigResult(
                    dialog.ExecutablePath ?? string.Empty,
                    dialog.LaunchArguments);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show game executable config dialog");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<LaunchProfileResult?> ShowLaunchProfileEditorAsync(
        LaunchProfile? existingProfile = null)
    {
        try
        {
            var dialog = new LaunchProfileEditorDialog();

            var profile = existingProfile ?? LaunchProfile.CreateBalancedProfile();
            dialog.SetProfile(profile);

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
            {
                _logger.LogWarning("Main window not found for launch profile editor dialog");
                return null;
            }

            var result = await dialog.ShowDialog<bool>(mainWindow);
            if (result && dialog.EditedProfile != null)
            {
                var edited = dialog.EditedProfile;
                return new LaunchProfileResult(
                    edited.Name,
                    edited.Description,
                    edited.Priority,
                    edited.ProcessesToSuspend,
                    edited.PerformanceSettings.EnableMemoryOptimization,
                    edited.PerformanceSettings.ClearStandbyList,
                    edited.PerformanceSettings.DisableVisualEffects);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show launch profile editor dialog");
            return null;
        }
    }
}
