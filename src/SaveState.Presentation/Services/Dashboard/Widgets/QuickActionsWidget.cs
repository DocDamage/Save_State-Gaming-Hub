using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Widget for quick actions like launching games, scanning, etc.
/// </summary>
public partial class QuickActionsWidget : WidgetBase
{
    private readonly INavigationService _navigationService;
    private readonly IOverlayService _overlayService;

    public QuickActionsWidget(
        INavigationService navigationService,
        IOverlayService overlayService,
        ILogger<QuickActionsWidget> logger)
        : base(logger)
    {
        _navigationService = navigationService;
        _overlayService = overlayService;
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
        // TODO: Navigate to the currently playing game
        await _navigationService.NavigateTo("Library");
    }

    /// <summary>
    /// Command to scan for new games.
    /// </summary>
    [RelayCommand]
    private void ScanForGames()
    {
        // TODO: Trigger game scanning
        _overlayService.ShowCommandPaletteOverlay();
    }

    /// <summary>
    /// Command to pick a random game.
    /// </summary>
    [RelayCommand]
    private async Task RandomGame()
    {
        // TODO: Navigate to random game
        await _navigationService.NavigateTo("Library");
    }

    /// <summary>
    /// Command to get AI recommendations.
    /// </summary>
    [RelayCommand]
    private void AiRecommend()
    {
        // TODO: Show AI recommendations
        _overlayService.ShowAiAssistantOverlay();
    }
}