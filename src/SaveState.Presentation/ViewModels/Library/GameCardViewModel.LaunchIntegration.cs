using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.Services.DTOs;
using SaveState.Presentation.ViewModels.Overlays;

namespace SaveState.Presentation.ViewModels.Library;

/// <summary>
/// Partial class for GameCardViewModel containing launch integration.
/// </summary>
public partial class GameCardViewModel
{
    private LaunchExperienceViewModel? _launchExperienceViewModel;
    private ILaunchExperienceManager? _launchExperienceManager;

    /// <summary>
    /// Gets or sets the launch experience view model (injected).
    /// </summary>
    public LaunchExperienceViewModel? LaunchExperienceViewModel
    {
        get => _launchExperienceViewModel;
        set
        {
            _launchExperienceViewModel = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Launches the game with optional cinematic launch experience.
    /// </summary>
    [RelayCommand]
    private async Task LaunchGameAsync()
    {
        if (_launchExperienceManager == null)
        {
            _logger.LogWarning("Launch experience manager not available, launching directly");
            await LaunchDirectAsync();
            return;
        }

        try
        {
            // Get configuration for this game
            var configResult = await _launchExperienceManager.GetLaunchExperienceConfigAsync(
                GameId.Value, 
                CancellationToken.None);

            // Check if cinematic launch is enabled (either globally or per-game)
            if (ShouldShowCinematicLaunch(configResult.Value))
            {
                await ShowCinematicLaunchAsync();
            }
            else
            {
                await LaunchDirectAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch game {Game}", Title);
            await LaunchDirectAsync();
        }
    }

    /// <summary>
    /// Determines whether to show the cinematic launch experience.
    /// </summary>
    private bool ShouldShowCinematicLaunch(LaunchExperienceConfig? config)
    {
        // If no config exists, use default behavior (enabled)
        if (config == null)
            return true;

        // Check if any visual elements are enabled
        return config.ShowGameFacts || 
               config.ShowLastProgress || 
               config.ShowAchievementProgress;
    }

    /// <summary>
    /// Shows the cinematic launch experience overlay.
    /// </summary>
    private async Task ShowCinematicLaunchAsync()
    {
        if (LaunchExperienceViewModel == null)
        {
            _logger.LogWarning("Launch experience view model not available");
            await LaunchDirectAsync();
            return;
        }

        // Start the launch sequence
        await LaunchExperienceViewModel.StartLaunchSequenceAsync(this);

        // Wait for completion or cancellation
        if (LaunchExperienceViewModel.IsCompleted)
        {
            await LaunchDirectAsync();
        }
        else
        {
            _logger.LogInformation("Launch cancelled for {Game}", Title);
        }
    }

    /// <summary>
    /// Launches the game directly without cinematic overlay.
    /// </summary>
    private async Task LaunchDirectAsync()
    {
        _logger.LogInformation("Launching game directly: {Title}", Title);
        
        // NOTE: This is a demo implementation. Replace with actual game launcher service call.
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Configures the launch experience for this game.
    /// </summary>
    [RelayCommand]
    private async Task ConfigureLaunchExperienceAsync()
    {
        // This would open the LaunchExperienceConfigDialog
        // and save settings specific to this game
        _logger.LogInformation("Configuring launch experience for {Game}", Title);
        
        await Task.CompletedTask;
    }
}
