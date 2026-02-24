// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.WebBrowser.Services;

namespace SaveState.Presentation.ViewModels.WebBrowser;

/// <summary>
/// ViewModel for the new tab page.
/// </summary>
public sealed partial class NewTabPageViewModel : ObservableObject
{
    private readonly IBrowserService _browserService;
    private readonly ILogger<NewTabPageViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ShortcutItemViewModel> _shortcuts = new();

    [ObservableProperty]
    private ObservableCollection<RecentItemViewModel> _recentlyVisited = new();

    public NewTabPageViewModel(
        IBrowserService browserService,
        ILogger<NewTabPageViewModel> logger,
        ITimeProvider? timeProvider = null)
    {
        _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? SystemTimeProvider.Instance;

        LoadDefaultShortcuts();
        _ = LoadRecentlyVisitedAsync();
    }

    private void LoadDefaultShortcuts()
    {
        Shortcuts.Add(new ShortcutItemViewModel
        {
            Title = "Google",
            Url = "https://www.google.com",
            Icon = "🔍"
        });
        Shortcuts.Add(new ShortcutItemViewModel
        {
            Title = "YouTube",
            Url = "https://www.youtube.com",
            Icon = "▶️"
        });
        Shortcuts.Add(new ShortcutItemViewModel
        {
            Title = "GitHub",
            Url = "https://github.com",
            Icon = "💻"
        });
        Shortcuts.Add(new ShortcutItemViewModel
        {
            Title = "Reddit",
            Url = "https://www.reddit.com",
            Icon = "🤖"
        });
        Shortcuts.Add(new ShortcutItemViewModel
        {
            Title = "Twitch",
            Url = "https://www.twitch.tv",
            Icon = "📺"
        });
        Shortcuts.Add(new ShortcutItemViewModel
        {
            Title = "Discord",
            Url = "https://discord.com",
            Icon = "💬"
        });
    }

    [RelayCommand]
    private async Task LoadRecentlyVisitedAsync()
    {
        try
        {
            var result = await _browserService.GetHistoryAsync(_timeProvider.Now.AddDays(-7), null);

            if (result.IsSuccess && result.Value != null)
            {
                RecentlyVisited.Clear();
                foreach (var item in result.Value.Take(10))
                {
                    RecentlyVisited.Add(new RecentItemViewModel
                    {
                        Title = item.Title,
                        Url = item.Url,
                        Id = item.Id
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recently visited");
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        try
        {
            var searchUrl = _browserService.CurrentSettings.SearchEngine + Uri.EscapeDataString(SearchQuery);
            await _browserService.CreateTabAsync(searchUrl, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform search");
        }
    }

    [RelayCommand]
    private async Task OpenShortcutAsync(ShortcutItemViewModel? shortcut)
    {
        if (shortcut == null) return;

        try
        {
            await _browserService.CreateTabAsync(shortcut.Url, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open shortcut");
        }
    }

    [RelayCommand]
    private async Task OpenRecentItemAsync(RecentItemViewModel? item)
    {
        if (item == null) return;

        try
        {
            await _browserService.CreateTabAsync(item.Url, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open recent item");
        }
    }

    [RelayCommand]
    private void RemoveRecentItem(RecentItemViewModel? item)
    {
        if (item == null) return;

        RecentlyVisited.Remove(item);
    }

    [RelayCommand]
    private void ClearRecentAsync()
    {
        RecentlyVisited.Clear();
    }

    [RelayCommand]
    private void ViewUpdatesAsync()
    {
        // Open the SaveState website or changelog
        _logger.LogInformation("View updates requested");
    }

    [RelayCommand]
    private void AddShortcutAsync()
    {
        // Open dialog to add a new shortcut
        _logger.LogInformation("Add shortcut requested");
    }
}

/// <summary>
/// ViewModel for a shortcut item.
/// </summary>
public sealed partial class ShortcutItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _icon = "🌐";
}

/// <summary>
/// ViewModel for a recent item.
/// </summary>
public sealed partial class RecentItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;
}
