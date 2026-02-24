using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the streaming overlay browser.
/// </summary>
public partial class StreamingBrowserOverlayViewModel : ObservableObject
{
    private readonly ILogger<StreamingBrowserOverlayViewModel> _logger;
    private readonly INotificationService _notificationService;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private string _currentUrl = "about:blank";

    [ObservableProperty]
    private string _pageTitle = "Streaming Browser";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private double _opacity = 0.9;

    [ObservableProperty]
    private bool _isAlwaysOnTop = true;

    [ObservableProperty]
    private bool _isClickThrough;

    [ObservableProperty]
    private bool _isCompactMode;

    [ObservableProperty]
    private bool _showChat = true;

    [ObservableProperty]
    private ObservableCollection<StreamChatMessage> _chatMessages = new();

    [ObservableProperty]
    private ObservableCollection<StreamQuickLink> _quickLinks = new();

    [ObservableProperty]
    private string _streamKey = string.Empty;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private string _streamingPlatform = "None";

    [ObservableProperty]
    private int _viewerCount;

    [ObservableProperty]
    private TimeSpan _streamDuration;

    [ObservableProperty]
    private bool _showPerformanceStats = true;

    [ObservableProperty]
    private PerformanceStats _performanceStats = new();

    public StreamingBrowserOverlayViewModel(
        ILogger<StreamingBrowserOverlayViewModel> logger,
        INotificationService notificationService,
        ITimeProvider? timeProvider = null)
    {
        _logger = logger;
        _notificationService = notificationService;
        _timeProvider = timeProvider ?? SystemTimeProvider.Instance;

        LoadQuickLinks();
        LoadMockChat();
    }

    private void LoadQuickLinks()
    {
        QuickLinks.Add(new StreamQuickLink
        {
            Name = "Twitch Dashboard",
            Url = "https://dashboard.twitch.tv",
            Icon = "💜"
        });

        QuickLinks.Add(new StreamQuickLink
        {
            Name = "YouTube Studio",
            Url = "https://studio.youtube.com",
            Icon = "❤️"
        });

        QuickLinks.Add(new StreamQuickLink
        {
            Name = "OBS Remote",
            Url = "http://localhost:4455",
            Icon = "⚫"
        });

        QuickLinks.Add(new StreamQuickLink
        {
            Name = "StreamElements",
            Url = "https://streamelements.com",
            Icon = "🔧"
        });

        QuickLinks.Add(new StreamQuickLink
        {
            Name = "Streamlabs",
            Url = "https://streamlabs.com",
            Icon = "🎨"
        });

        QuickLinks.Add(new StreamQuickLink
        {
            Name = "Spotify",
            Url = "https://open.spotify.com",
            Icon = "🎵"
        });

        QuickLinks.Add(new StreamQuickLink
        {
            Name = "Discord",
            Url = "https://discord.com/app",
            Icon = "💬"
        });

        QuickLinks.Add(new StreamQuickLink
        {
            Name = "Twitter/X",
            Url = "https://twitter.com",
            Icon = "🐦"
        });
    }

    private void LoadMockChat()
    {
        ChatMessages.Add(new StreamChatMessage
        {
            Username = "Viewer1",
            Message = "Great stream!",
            Color = "#FF6B6B",
            IsModerator = false,
            IsSubscriber = true,
            Timestamp = _timeProvider.Now.AddMinutes(-5)
        });

        ChatMessages.Add(new StreamChatMessage
        {
            Username = "ModUser",
            Message = "Welcome everyone!",
            Color = "#4ECDC4",
            IsModerator = true,
            IsSubscriber = true,
            Timestamp = _timeProvider.Now.AddMinutes(-3)
        });

        ChatMessages.Add(new StreamChatMessage
        {
            Username = "NewViewer",
            Message = "First time here, this game looks awesome!",
            Color = "#95E1D3",
            IsModerator = false,
            IsSubscriber = false,
            Timestamp = _timeProvider.Now.AddMinutes(-1)
        });
    }

    [RelayCommand]
    private void NavigateTo(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        CurrentUrl = url;
        _logger.LogInformation("Navigating to: {Url}", url);
    }

    [RelayCommand]
    private void NavigateToQuickLink(StreamQuickLink link)
    {
        if (link == null) return;

        CurrentUrl = link.Url;
        PageTitle = link.Name;
    }

    [RelayCommand]
    private void ToggleChat()
    {
        ShowChat = !ShowChat;
        _notificationService.ShowInfo(ShowChat ? "Chat shown" : "Chat hidden");
    }

    [RelayCommand]
    private void ToggleClickThrough()
    {
        IsClickThrough = !IsClickThrough;
        _notificationService.ShowInfo(IsClickThrough ? "Click-through enabled" : "Click-through disabled");
    }

    [RelayCommand]
    private void ToggleAlwaysOnTop()
    {
        IsAlwaysOnTop = !IsAlwaysOnTop;
        _notificationService.ShowInfo(IsAlwaysOnTop ? "Always on top enabled" : "Always on top disabled");
    }

    [RelayCommand]
    private void ToggleCompactMode()
    {
        IsCompactMode = !IsCompactMode;
    }

    [RelayCommand]
    private void AdjustOpacity(double delta)
    {
        Opacity = Math.Clamp(Opacity + delta, 0.1, 1.0);
    }

    [RelayCommand]
    private void SendChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        ChatMessages.Add(new StreamChatMessage
        {
            Username = "Streamer",
            Message = message,
            Color = "#FFD93D",
            IsStreamer = true,
            Timestamp = _timeProvider.Now
        });
    }

    [RelayCommand]
    private void StartStream()
    {
        IsStreaming = true;
        StreamingPlatform = "Twitch";
        _notificationService.ShowSuccess("Stream started!");
        _logger.LogInformation("Stream started on {Platform}", StreamingPlatform);
    }

    [RelayCommand]
    private void StopStream()
    {
        IsStreaming = false;
        StreamingPlatform = "None";
        ViewerCount = 0;
        _notificationService.ShowInfo("Stream stopped");
    }

    [RelayCommand]
    private void OpenStreamSettings()
    {
        _logger.LogDebug("Opening stream settings");
    }

    [RelayCommand]
    private void MuteChatUser(string username)
    {
        _notificationService.ShowInfo($"Muted user: {username}");
        _logger.LogInformation("Muted user: {Username}", username);
    }

    [RelayCommand]
    private void BanChatUser(string username)
    {
        _notificationService.ShowWarning($"Banned user: {username}");
        _logger.LogWarning("Banned user: {Username}", username);
    }

    [RelayCommand]
    private void TimeoutChatUser(string username)
    {
        _notificationService.ShowInfo($"Timed out user: {username} (5 min)");
    }

    [RelayCommand]
    private void ClearChat()
    {
        ChatMessages.Clear();
        _notificationService.ShowInfo("Chat cleared");
    }

    [RelayCommand]
    private void TogglePerformanceStats()
    {
        ShowPerformanceStats = !ShowPerformanceStats;
    }

    [RelayCommand]
    private void CloseOverlay()
    {
        // Signal to close the overlay
    }

    [RelayCommand]
    private void MinimizeOverlay()
    {
        IsCompactMode = true;
    }
}

/// <summary>
/// Represents a stream chat message.
/// </summary>
public class StreamChatMessage
{
    public string Username { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Color { get; set; } = "#FFFFFF";
    public bool IsModerator { get; set; }
    public bool IsSubscriber { get; set; }
    public bool IsStreamer { get; set; }
    public DateTime Timestamp { get; set; }

    public string Badge => IsStreamer ? "🔴" : IsModerator ? "⚔️" : IsSubscriber ? "⭐" : "";
}

/// <summary>
/// Represents a quick link for streamers.
/// </summary>
public class StreamQuickLink
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "🔗";
}

/// <summary>
/// Performance statistics for streaming.
/// </summary>
public class PerformanceStats
{
    public int Fps { get; set; } = 60;
    public int CpuUsage { get; set; } = 25;
    public int GpuUsage { get; set; } = 45;
    public int RamUsage { get; set; } = 4096;
    public int StreamBitrate { get; set; } = 6000;
    public int DroppedFrames { get; set; } = 0;
    public double NetworkLatency { get; set; } = 12.5;
}
