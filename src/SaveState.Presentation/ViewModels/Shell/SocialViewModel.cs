using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Social.Entities;
using SaveState.Core.Social.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Social tab.
/// </summary>
public partial class SocialViewModel : ObservableObject
{
    private readonly IFriendActivityService _friendActivityService;
    private readonly ILogger<SocialViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FriendActivityViewModel> _activityFeed = new();

    [ObservableProperty]
    private ObservableCollection<FriendViewModel> _allFriends = new();

    [ObservableProperty]
    private ObservableCollection<FriendViewModel> _onlineFriends = new();

    [ObservableProperty]
    private string _selectedFilter = "All";

    // Statistics
    [ObservableProperty]
    private int _totalFriends;

    [ObservableProperty]
    private int _onlineCount;

    [ObservableProperty]
    private int _totalActivities;

    [ObservableProperty]
    private int _todayActivities;

    [ObservableProperty]
    private string _mostPlayedGame = "N/A";

    [ObservableProperty]
    private bool _isDiscordConnected;

    [ObservableProperty]
    private bool _isSteamConnected;

    public SocialViewModel(
        IFriendActivityService friendActivityService,
        ILogger<SocialViewModel> logger,
        ITimeProvider timeProvider)
    {
        _friendActivityService = friendActivityService;
        _logger = logger;
        _timeProvider = timeProvider;

        // Load data when ViewModel is created (fire-and-forget)
        _ = LoadSocialDataAsync();
    }

    /// <summary>
    /// Gets the display title for the social tab.
    /// </summary>
    public string Title => "👥 Social Hub";

    public ObservableCollection<string> FilterOptions { get; } = new()
    {
        "All",
        "Online",
        "Playing",
        "Discord",
        "Steam"
    };

    /// <summary>
    /// Loads all social data.
    /// </summary>
    [RelayCommand]
    private async Task LoadSocialDataAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await Task.WhenAll(
                LoadActivityFeedAsync(),
                LoadFriendsAsync(),
                LoadStatisticsAsync()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load social data");
            ErrorMessage = "Failed to load social data. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads the activity feed.
    /// </summary>
    [RelayCommand]
    private async Task LoadActivityFeedAsync()
    {
        var result = await _friendActivityService.GetActivityFeedAsync(50);

        if (result.IsSuccess && result.Value != null)
        {
            ActivityFeed.Clear();
            foreach (var activity in result.Value)
            {
                ActivityFeed.Add(new FriendActivityViewModel(activity, _timeProvider));
            }

            TodayActivities = ActivityFeed.Count(a => a.IsToday);
        }
    }

    /// <summary>
    /// Loads friends list.
    /// </summary>
    [RelayCommand]
    private async Task LoadFriendsAsync()
    {
        var result = await _friendActivityService.GetFriendsAsync();

        if (result.IsSuccess && result.Value != null)
        {
            AllFriends.Clear();
            OnlineFriends.Clear();

            foreach (var friend in result.Value)
            {
                var friendVm = new FriendViewModel(friend, _timeProvider);
                AllFriends.Add(friendVm);

                if (friend.IsOnline)
                {
                    OnlineFriends.Add(friendVm);
                }
            }

            TotalFriends = AllFriends.Count;
            OnlineCount = OnlineFriends.Count;
        }
    }

    /// <summary>
    /// Loads social statistics.
    /// </summary>
    [RelayCommand]
    private async Task LoadStatisticsAsync()
    {
        var result = await _friendActivityService.GetStatisticsAsync();

        if (result.IsSuccess && result.Value != null)
        {
            var stats = result.Value;
            TotalActivities = stats.TotalActivities;
            // MostPlayedGame would need to be calculated separately from game sessions
        }
    }

    /// <summary>
    /// Syncs friends from Discord.
    /// </summary>
    [RelayCommand]
    private async Task SyncDiscordAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _friendActivityService.SyncDiscordFriendsAsync();

            if (result.IsSuccess)
            {
                IsDiscordConnected = true;
                await LoadFriendsAsync();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to sync Discord friends";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Discord friends");
            ErrorMessage = "Failed to connect to Discord. Please check your settings.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Syncs friends from Steam.
    /// </summary>
    [RelayCommand]
    private async Task SyncSteamAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _friendActivityService.SyncSteamFriendsAsync();

            if (result.IsSuccess)
            {
                IsSteamConnected = true;
                await LoadFriendsAsync();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to sync Steam friends";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync Steam friends");
            ErrorMessage = "Failed to connect to Steam. Please check your settings.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Updates friend statuses.
    /// </summary>
    [RelayCommand]
    private async Task RefreshStatusesAsync()
    {
        var result = await _friendActivityService.UpdateFriendStatusesAsync();

        if (result.IsSuccess)
        {
            await LoadFriendsAsync();
        }
    }

    /// <summary>
    /// Refreshes all social data.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadSocialDataAsync();
    }

    /// <summary>
    /// Applies the selected filter to the friends list.
    /// </summary>
    [RelayCommand]
    private void ApplyFilter(string filter)
    {
        SelectedFilter = filter;
        // Filter logic would be implemented here
        // For now, just update the selected filter
    }
}

// View Models for nested data
public class FriendActivityViewModel
{
    public FriendActivityViewModel(FriendActivity activity, ITimeProvider timeProvider)
    {
        FriendName = activity.Friend.Name;
        AvatarUrl = activity.Friend.AvatarUrl;
        Type = activity.Type;
        GameTitle = activity.GameTitle;
        Details = activity.Details;
        Timestamp = activity.Timestamp;
        Platform = activity.Platform;

        // Format activity message
        ActivityMessage = Type switch
        {
            ActivityType.StartedPlaying => $"started playing {GameTitle}",
            ActivityType.StoppedPlaying => $"stopped playing {GameTitle}",
            ActivityType.UnlockedAchievement => $"unlocked an achievement in {GameTitle}",
            ActivityType.CompletedGame => $"completed {GameTitle}! 🎉",
            ActivityType.AddedToLibrary => $"added {GameTitle} to their library",
            ActivityType.WroteReview => $"wrote a review for {GameTitle}",
            ActivityType.JoinedMultiplayer => $"joined multiplayer in {GameTitle}",
            _ => $"activity in {GameTitle}"
        };

        // Format timestamp
        var elapsed = timeProvider.UtcNow - Timestamp;
        TimeAgo = elapsed.TotalMinutes < 1 ? "just now" :
                  elapsed.TotalMinutes < 60 ? $"{(int)elapsed.TotalMinutes}m ago" :
                  elapsed.TotalHours < 24 ? $"{(int)elapsed.TotalHours}h ago" :
                  elapsed.TotalDays < 7 ? $"{(int)elapsed.TotalDays}d ago" :
                  Timestamp.ToString("MMM dd");

        IsToday = Timestamp.Date == timeProvider.UtcNow.Date;

        // Activity icon
        ActivityIcon = Type switch
        {
            ActivityType.StartedPlaying => "▶️",
            ActivityType.StoppedPlaying => "⏸️",
            ActivityType.UnlockedAchievement => "🏆",
            ActivityType.CompletedGame => "✅",
            ActivityType.AddedToLibrary => "➕",
            ActivityType.WroteReview => "✍️",
            ActivityType.JoinedMultiplayer => "👥",
            _ => "📝"
        };

        PlatformIcon = Platform switch
        {
            SocialPlatform.Discord => "🎮",
            SocialPlatform.Steam => "🎮",
            _ => "🌐"
        };
    }

    public string FriendName { get; }
    public string? AvatarUrl { get; }
    public ActivityType Type { get; }
    public string GameTitle { get; }
    public string? Details { get; }
    public DateTime Timestamp { get; }
    public SocialPlatform Platform { get; }
    public string ActivityMessage { get; }
    public string TimeAgo { get; }
    public bool IsToday { get; }
    public string ActivityIcon { get; }
    public string PlatformIcon { get; }
}

public class FriendViewModel
{
    public FriendViewModel(Friend friend, ITimeProvider timeProvider)
    {
        Id = friend.Id;
        Name = friend.Name;
        AvatarUrl = friend.AvatarUrl;
        Platform = friend.Platform;
        IsOnline = friend.IsOnline;
        CurrentGame = friend.CurrentGame;
        LastSeenAt = friend.LastSeenAt;

        StatusColor = IsOnline ? "#44cc11" : "#888888";
        StatusText = IsOnline ? "Online" : "Offline";

        if (IsOnline && !string.IsNullOrEmpty(CurrentGame))
        {
            StatusText = $"Playing {CurrentGame}";
        }
        else if (LastSeenAt.HasValue)
        {
            var elapsed = timeProvider.UtcNow - LastSeenAt.Value;
            if (elapsed.TotalMinutes < 60)
                StatusText = $"Last seen {(int)elapsed.TotalMinutes}m ago";
            else if (elapsed.TotalHours < 24)
                StatusText = $"Last seen {(int)elapsed.TotalHours}h ago";
            else if (elapsed.TotalDays < 7)
                StatusText = $"Last seen {(int)elapsed.TotalDays}d ago";
        }

        PlatformIcon = Platform switch
        {
            SocialPlatform.Discord => "🎮",
            SocialPlatform.Steam => "🎮",
            _ => "🌐"
        };
    }

    public Guid Id { get; }
    public string Name { get; }
    public string? AvatarUrl { get; }
    public SocialPlatform Platform { get; }
    public bool IsOnline { get; }
    public string? CurrentGame { get; }
    public DateTime? LastSeenAt { get; }
    public string StatusColor { get; }
    public string StatusText { get; }
    public string PlatformIcon { get; }
}

public class FriendActivityStatistics
{
    public int TotalActivities { get; set; }
    public string? MostPlayedGame { get; set; }
}
