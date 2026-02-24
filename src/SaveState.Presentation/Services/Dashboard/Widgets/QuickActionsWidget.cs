using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using SaveState.Core.GameLibrary;
using System.Linq;
using System.Collections.Generic;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Widget for quick actions like launching games, scanning, etc.
/// </summary>
public partial class QuickActionsWidget : WidgetBase
{
    private readonly INavigationService _navigationService;
    private readonly IOverlayService _overlayService;
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionRepository _gameSessionRepository;

    public QuickActionsWidget(
        INavigationService navigationService,
        IOverlayService overlayService,
        IGameRepository gameRepository,
        IGameSessionRepository gameSessionRepository,
        ILogger<QuickActionsWidget> logger)
        : base(logger)
    {
        _navigationService = navigationService;
        _overlayService = overlayService;
        _gameRepository = gameRepository;
        _gameSessionRepository = gameSessionRepository;
    }

    /// <inheritdoc />
    public override string Id => "quick-actions";

    /// <inheritdoc />
    public override string Title => "Quick Actions";

    /// <inheritdoc />
    public override string Icon => "🚀";

    /// <inheritdoc />
    public override WidgetSize DefaultSize => WidgetSize.Medium;

    /// <inheritdoc />
    public override WidgetSize[] SupportedSizes => new[] { WidgetSize.Medium, WidgetSize.Large };

    /// <inheritdoc />
    public override int RefreshIntervalMs => -1; // No auto-refresh needed

    /// <inheritdoc />
    public override bool CanMinimize => false;

    /// <inheritdoc />
    public override bool CanRemove => false;

    /// <summary>
    /// Command to continue playing the last game.
    /// </summary>
    [RelayCommand]
    private async Task ContinuePlaying()
    {
        try
        {
            var sessions = await _gameSessionRepository.GetRecentSessionsAsync(1);
            var lastSession = sessions.OrderByDescending(s => s.EndedAt ?? s.StartedAt).FirstOrDefault();

            if (lastSession != null)
            {
                 var gameId = SaveState.Core.Common.ValueObjects.GameId.From(lastSession.GameId);
                 await _navigationService.NavigateToAsync("Library", gameId);
            }
            else
            {
                 // No recent games, just go to library
                 await _navigationService.NavigateToAsync("Library");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to navigate to last played game");
            try
            {
                await _navigationService.NavigateToAsync("Library");
            }
            catch (Exception navigationEx)
            {
                Logger.LogError(navigationEx, "Fallback navigation to library failed");
            }
        }
    }

    /// <summary>
    /// Command to scan for new games.
    /// </summary>
    [RelayCommand]
    private void ScanForGames()
    {
        // Open Command Palette with Scan preset
        _overlayService.ShowCommandPaletteOverlay();
    }

    /// <summary>
    /// Command to pick a random game.
    /// </summary>
    [RelayCommand]
    private async Task RandomGame()
    {
        try
        {
            var allGames = await _gameRepository.GetAllAsync();
            if (allGames.Any())
            {
                var random = new Random();
                var game = allGames[random.Next(allGames.Count)];
                await _navigationService.NavigateToAsync("Library", SaveState.Core.Common.ValueObjects.GameId.From(game.Id));
            }
            else
            {
                await _navigationService.NavigateToAsync("Library");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to pick random game");
            try
            {
                await _navigationService.NavigateToAsync("Library");
            }
            catch (Exception navigationEx)
            {
                Logger.LogError(navigationEx, "Fallback navigation to library failed");
            }
        }
    }

    /// <summary>
    /// Command to get AI recommendations.
    /// </summary>
    [RelayCommand]
    private void AiRecommend()
    {
        _overlayService.ShowAiAssistantOverlay();
    }
}
