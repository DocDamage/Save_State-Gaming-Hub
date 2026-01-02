using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Widget showing recently added games.
/// </summary>
public partial class RecentlyAddedWidget : WidgetBase
{
    private readonly INavigationService _navigationService;

    public RecentlyAddedWidget(
        INavigationService navigationService,
        ILogger<RecentlyAddedWidget> logger)
        : base(logger)
    {
        _navigationService = navigationService;
        RecentlyAddedGames = new ObservableCollection<Game>();
    }

    /// <inheritdoc />
    public override string Id => "recently-added";

    /// <inheritdoc />
    public override string Title => "Recently Added";

    /// <inheritdoc />
    public override string Icon => "🆕";

    /// <inheritdoc />
    public override WidgetSize DefaultSize => WidgetSize.Medium;

    /// <inheritdoc />
    public override WidgetSize[] SupportedSizes => new[] { WidgetSize.Medium, WidgetSize.Large };

    /// <inheritdoc />
    public override int RefreshIntervalMs => 300000; // 5 minutes

    /// <summary>
    /// Gets the collection of recently added games.
    /// </summary>
    public ObservableCollection<Game> RecentlyAddedGames { get; }

    /// <inheritdoc />
    protected override async Task LoadDataAsync()
    {
        RecentlyAddedGames.Clear();

        // TODO: Get real recently added games from repository
        // For now, simulate some games
        RecentlyAddedGames.Add(Core.GameLibrary.Entities.Game.Create("Elden Ring"));
        RecentlyAddedGames.Add(Core.GameLibrary.Entities.Game.Create("Cyberpunk 2077"));
        RecentlyAddedGames.Add(Core.GameLibrary.Entities.Game.Create("Hollow Knight"));

        await Task.CompletedTask; // Simulate async operation
    }

    /// <summary>
    /// Command to navigate to the library.
    /// </summary>
    [RelayCommand]
    private void NavigateToLibrary()
    {
        _navigationService.NavigateTo("Library");
    }

    /// <summary>
    /// Command to view a specific game.
    /// </summary>
    [RelayCommand]
    private void ViewGame(Game game)
    {
        // TODO: Navigate to game detail view
        _navigationService.NavigateTo("Library");
    }
}