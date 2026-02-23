using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.WebBrowser;

/// <summary>
/// ViewModel for the game guide browser feature.
/// </summary>
public partial class GameGuideViewModel : ObservableObject
{
    private readonly ILogger<GameGuideViewModel> _logger;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private Game? _currentGame;

    [ObservableProperty]
    private ObservableCollection<GuideSource> _guideSources = new();

    [ObservableProperty]
    private GuideSource? _selectedSource;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<GuideSearchResult> _searchResults = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _currentUrl = "about:blank";

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private bool _isLoadingPage;

    [ObservableProperty]
    private double _loadingProgress;

    public GameGuideViewModel(
        ILogger<GameGuideViewModel> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Loads guide sources for the specified game.
    /// </summary>
    public void LoadGuidesForGame(Game game)
    {
        CurrentGame = game;
        GuideSources.Clear();

        var gameName = game.Title;
        var gameSlug = CreateSlug(gameName);
        var escapedName = Uri.EscapeDataString(gameName);

        // Add default guide sources
        GuideSources.Add(new GuideSource
        {
            Name = "Wiki (Fandom)",
            Url = $"https://{gameSlug.Replace("-", "")}.fandom.com/wiki/{gameSlug.Replace("-", "_")}",
            Icon = "📚",
            Description = "Community wiki with comprehensive game information"
        });

        GuideSources.Add(new GuideSource
        {
            Name = "IGN",
            Url = $"https://www.ign.com/wikis/{gameSlug}",
            Icon = "🎮",
            Description = "IGN walkthroughs and guides"
        });

        GuideSources.Add(new GuideSource
        {
            Name = "GameFAQs",
            Url = $"https://gamefaqs.gamespot.com/search?game={escapedName}",
            Icon = "❓",
            Description = "User-contributed guides and FAQs"
        });

        GuideSources.Add(new GuideSource
        {
            Name = "Steam Guides",
            Url = game.StoreId != null && game.Platform?.Name == "Steam"
                ? $"https://steamcommunity.com/app/{game.StoreId}/guides/"
                : $"https://steamcommunity.com/search/?text={escapedName}&filter=guides",
            Icon = "🎯",
            Description = "Community guides from Steam"
        });

        GuideSources.Add(new GuideSource
        {
            Name = "YouTube",
            Url = $"https://www.youtube.com/results?search_query={escapedName}+walkthrough+guide",
            Icon = "▶",
            Description = "Video walkthroughs and tutorials"
        });

        GuideSources.Add(new GuideSource
        {
            Name = "Reddit",
            Url = $"https://www.reddit.com/search/?q={escapedName}+guide&type=posts",
            Icon = "🤖",
            Description = "Reddit community discussions and tips"
        });

        GuideSources.Add(new GuideSource
        {
            Name = "Polygon",
            Url = $"https://www.polygon.com/search?q={escapedName}&type=Article",
            Icon = "📰",
            Description = "Polygon articles and guides"
        });

        GuideSources.Add(new GuideSource
        {
            Name = "PCGamer",
            Url = $"https://www.pcgamer.com/search/?searchTerm={escapedName}",
            Icon = "💻",
            Description = "PC Gamer guides and tips"
        });

        GuideSources.Add(new GuideSource
        {
            Name = "TrueAchievements",
            Url = $"https://www.trueachievements.com/searchresults.aspx?search={escapedName}",
            Icon = "🏆",
            Description = "Achievement guides and tracking"
        });

        GuideSources.Add(new GuideSource
        {
            Name = "Speedrun.com",
            Url = $"https://www.speedrun.com/search?game={escapedName}",
            Icon = "⚡",
            Description = "Speedrunning guides and routes"
        });

        // Select first source by default
        SelectedSource = GuideSources.FirstOrDefault();
        if (SelectedSource != null)
        {
            CurrentUrl = SelectedSource.Url;
        }

        _logger.LogInformation("Loaded {Count} guide sources for {Game}", GuideSources.Count, game.Title);
    }

    /// <summary>
    /// Creates a URL-friendly slug from a game name.
    /// </summary>
    private static string CreateSlug(string gameName)
    {
        return gameName
            .ToLowerInvariant()
            .Replace(":", "")
            .Replace("'", "")
            .Replace("&", "and")
            .Replace("  ", " ")
            .Trim()
            .Replace(" ", "-");
    }

    [RelayCommand]
    private void SelectSource(GuideSource source)
    {
        if (source == null) return;

        SelectedSource = source;
        CurrentUrl = source.Url;
        _logger.LogInformation("Selected guide source: {Source}", source.Name);
    }

    [RelayCommand]
    private async Task SearchGuidesAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) || CurrentGame == null)
            return;

        IsLoading = true;
        SearchResults.Clear();

        try
        {
            var query = Uri.EscapeDataString($"{CurrentGame.Title} {SearchQuery} guide");

            // Add search results from different sources
            SearchResults.Add(new GuideSearchResult
            {
                Title = $"Search '{SearchQuery}' on Fandom",
                Url = $"https://{CreateSlug(CurrentGame.Title).Replace("-", "")}.fandom.com/wiki/Special:Search?query={Uri.EscapeDataString(SearchQuery)}",
                Source = "Fandom Wiki",
                Type = "Wiki"
            });

            SearchResults.Add(new GuideSearchResult
            {
                Title = $"Search '{SearchQuery}' on YouTube",
                Url = $"https://www.youtube.com/results?search_query={query}",
                Source = "YouTube",
                Type = "Video"
            });

            SearchResults.Add(new GuideSearchResult
            {
                Title = $"Search '{SearchQuery}' on Reddit",
                Url = $"https://www.reddit.com/search/?q={query}&type=posts",
                Source = "Reddit",
                Type = "Discussion"
            });

            _notificationService.ShowInfo($"Found {SearchResults.Count} search results");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void NavigateBack()
    {
        // Would integrate with browser control
        _logger.LogDebug("Navigate back requested");
    }

    [RelayCommand]
    private void NavigateForward()
    {
        // Would integrate with browser control
        _logger.LogDebug("Navigate forward requested");
    }

    [RelayCommand]
    private void Refresh()
    {
        // Would integrate with browser control
        _logger.LogDebug("Refresh requested");
    }

    [RelayCommand]
    private void StopLoading()
    {
        IsLoadingPage = false;
        _logger.LogDebug("Stop loading requested");
    }

    [RelayCommand]
    private void GoHome()
    {
        if (SelectedSource != null)
        {
            CurrentUrl = SelectedSource.Url;
        }
    }

    [RelayCommand]
    private void OpenInExternalBrowser(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open URL in external browser: {Url}", url);
            _notificationService.ShowError("Failed to open external browser");
        }
    }

    [RelayCommand]
    private void AddToFavorites(GuideSource source)
    {
        source.IsFavorite = !source.IsFavorite;
        _notificationService.ShowInfo(source.IsFavorite ? "Added to favorites" : "Removed from favorites");
    }

    /// <summary>
    /// Updates the browser navigation state.
    /// </summary>
    public void UpdateNavigationState(bool canGoBack, bool canGoForward)
    {
        CanGoBack = canGoBack;
        CanGoForward = canGoForward;
    }

    /// <summary>
    /// Updates the loading progress.
    /// </summary>
    public void UpdateLoadingProgress(double progress)
    {
        LoadingProgress = progress;
    }
}

/// <summary>
/// Represents a guide source (website) for a game.
/// </summary>
public class GuideSource
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "📄";
    public string Description { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
}

/// <summary>
/// Represents a search result for guides.
/// </summary>
public class GuideSearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Description { get; set; }
}
