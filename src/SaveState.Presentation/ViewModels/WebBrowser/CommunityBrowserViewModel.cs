using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.WebBrowser;

/// <summary>
/// ViewModel for the community browser feature.
/// </summary>
public partial class CommunityBrowserViewModel : ObservableObject
{
    private readonly ILogger<CommunityBrowserViewModel> _logger;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<CommunitySection> _sections = new();

    [ObservableProperty]
    private CommunitySection? _selectedSection;

    [ObservableProperty]
    private string _currentUrl = "about:blank";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CommunityPost> _recentPosts = new();

    [ObservableProperty]
    private ObservableCollection<TournamentListing> _tournaments = new();

    [ObservableProperty]
    private ObservableCollection<SharedSaveState> _sharedSaveStates = new();

    [ObservableProperty]
    private ObservableCollection<UserContent> _userContent = new();

    public CommunityBrowserViewModel(
        ILogger<CommunityBrowserViewModel> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;

        LoadSections();
        LoadMockData();
    }

    private void LoadSections()
    {
        Sections.Add(new CommunitySection
        {
            Name = "Official Forums",
            Icon = "💬",
            Url = "https://forums.savestate.reborn",
            Description = "Official SaveState community forums"
        });

        Sections.Add(new CommunitySection
        {
            Name = "Discord",
            Icon = "🎮",
            Url = "https://discord.gg/savestate",
            Description = "Join our Discord server"
        });

        Sections.Add(new CommunitySection
        {
            Name = "Reddit",
            Icon = "🤖",
            Url = "https://reddit.com/r/savestate",
            Description = "Reddit community"
        });

        Sections.Add(new CommunitySection
        {
            Name = "Save State Sharing",
            Icon = "💾",
            Url = "https://share.savestate.reborn",
            Description = "Share and download save states"
        });

        Sections.Add(new CommunitySection
        {
            Name = "Tournaments",
            Icon = "🏆",
            Url = "https://tournaments.savestate.reborn",
            Description = "Community tournaments and events"
        });

        Sections.Add(new CommunitySection
        {
            Name = "User Content",
            Icon = "🎨",
            Url = "https://content.savestate.reborn",
            Description = "Themes, plugins, and custom content"
        });

        Sections.Add(new CommunitySection
        {
            Name = "Wiki",
            Icon = "📚",
            Url = "https://wiki.savestate.reborn",
            Description = "Community-maintained wiki"
        });

        Sections.Add(new CommunitySection
        {
            Name = "GitHub",
            Icon = "💻",
            Url = "https://github.com/savestate/reborn",
            Description = "Open source contributions"
        });

        SelectedSection = Sections.FirstOrDefault();
        if (SelectedSection != null)
        {
            CurrentUrl = SelectedSection.Url;
        }
    }

    private void LoadMockData()
    {
        // Recent posts
        RecentPosts.Add(new CommunityPost
        {
            Title = "Best MUGEN characters for beginners?",
            Author = "FightingFan99",
            Replies = 24,
            Views = 342,
            PostedAt = DateTime.Now.AddHours(-2),
            Category = "MUGEN"
        });

        RecentPosts.Add(new CommunityPost
        {
            Title = "Share your RetroArch shader configs",
            Author = "RetroGamer42",
            Replies = 56,
            Views = 891,
            PostedAt = DateTime.Now.AddHours(-5),
            Category = "Retro Gaming"
        });

        RecentPosts.Add(new CommunityPost
        {
            Title = "Cloud sync not working with OneDrive",
            Author = "TechSupportPlz",
            Replies = 12,
            Views = 156,
            PostedAt = DateTime.Now.AddHours(-8),
            Category = "Support"
        });

        RecentPosts.Add(new CommunityPost
        {
            Title = "Created a dark theme - feedback welcome!",
            Author = "ThemeCreator_X",
            Replies = 38,
            Views = 523,
            PostedAt = DateTime.Now.AddHours(-12),
            Category = "Themes"
        });

        RecentPosts.Add(new CommunityPost
        {
            Title = "Elden Ring 100% completion save state",
            Author = "Tarnished_One",
            Replies = 89,
            Views = 1247,
            PostedAt = DateTime.Now.AddDays(-1),
            Category = "Save States"
        });

        // Tournaments
        Tournaments.Add(new TournamentListing
        {
            Name = "SaveState Fighting Championship",
            Game = "MUGEN",
            StartDate = DateTime.Now.AddDays(3),
            Prize = "$500",
            Participants = 64,
            MaxParticipants = 128,
            Status = TournamentStatus.Open
        });

        Tournaments.Add(new TournamentListing
        {
            Name = "Retro Speedrun Challenge",
            Game = "Super Mario World",
            StartDate = DateTime.Now.AddDays(7),
            Prize = "$200",
            Participants = 32,
            MaxParticipants = 100,
            Status = TournamentStatus.Open
        });

        Tournaments.Add(new TournamentListing
        {
            Name = "Weekly Race Night",
            Game = "Mario Kart 8",
            StartDate = DateTime.Now.AddDays(1),
            Prize = "Trophy + Badge",
            Participants = 48,
            MaxParticipants = 64,
            Status = TournamentStatus.AlmostFull
        });

        Tournaments.Add(new TournamentListing
        {
            Name = "Elden Ring PvP Invitational",
            Game = "Elden Ring",
            StartDate = DateTime.Now.AddDays(14),
            Prize = "$1000",
            Participants = 16,
            MaxParticipants = 32,
            Status = TournamentStatus.Open
        });

        // Shared save states
        SharedSaveStates.Add(new SharedSaveState
        {
            Title = "Elden Ring - NG+7 All Bosses",
            Game = "Elden Ring",
            Author = "SoulsMaster",
            Downloads = 3421,
            Rating = 4.8,
            Tags = new List<string> { "Endgame", "Max Level", "All Items" },
            ThumbnailUrl = null
        });

        SharedSaveStates.Add(new SharedSaveState
        {
            Title = "Hollow Knight - 112% Completion",
            Game = "Hollow Knight",
            Author = "BugHunter",
            Downloads = 2156,
            Rating = 4.9,
            Tags = new List<string> { "Completion", "All Charms", "Godhome" },
            ThumbnailUrl = null
        });

        SharedSaveStates.Add(new SharedSaveState
        {
            Title = "Baldur's Gate 3 - Dark Urge Ending",
            Game = "Baldur's Gate 3",
            Author = "MindFlayerFan",
            Downloads = 1892,
            Rating = 4.7,
            Tags = new List<string> { "Spoiler", "Ending", "Dark Urge" },
            ThumbnailUrl = null
        });

        SharedSaveStates.Add(new SharedSaveState
        {
            Title = "Celeste - All Strawberries + B-Sides",
            Game = "Celeste",
            Author = "ClimberPro",
            Downloads = 1567,
            Rating = 4.9,
            Tags = new List<string> { "100%", "B-Sides", "Gold Strawberries" },
            ThumbnailUrl = null
        });

        SharedSaveStates.Add(new SharedSaveState
        {
            Title = "Hades - 50 Heat Clear",
            Game = "Hades",
            Author = "SpeedRunner_X",
            Downloads = 1234,
            Rating = 4.6,
            Tags = new List<string> { "High Heat", "Skelly", "Endgame" },
            ThumbnailUrl = null
        });

        // User content
        UserContent.Add(new UserContent
        {
            Title = "Neon Nights Theme",
            Type = ContentType.Theme,
            Author = "CyberPunk_Gamer",
            Downloads = 5234,
            Rating = 4.7,
            PreviewUrl = null,
            Description = "Cyberpunk-inspired dark theme with neon accents"
        });

        UserContent.Add(new UserContent
        {
            Title = "Retro Wave Shader Pack",
            Type = ContentType.Shader,
            Author = "VaporWave_1999",
            Downloads = 3891,
            Rating = 4.8,
            PreviewUrl = null,
            Description = "80s retro aesthetic shaders for RetroArch"
        });

        UserContent.Add(new UserContent
        {
            Title = "Achievement Tracker Plugin",
            Type = ContentType.Plugin,
            Author = "Dev_User42",
            Downloads = 2156,
            Rating = 4.5,
            PreviewUrl = null,
            Description = "Advanced achievement tracking with statistics"
        });

        UserContent.Add(new UserContent
        {
            Title = "MUGEN Character Pack: Anime Legends",
            Type = ContentType.Character,
            Author = "AnimeFighter_X",
            Downloads = 8934,
            Rating = 4.9,
            PreviewUrl = null,
            Description = "20+ high-quality anime characters for MUGEN"
        });
    }

    [RelayCommand]
    private void SelectSection(CommunitySection section)
    {
        if (section == null) return;

        SelectedSection = section;
        CurrentUrl = section.Url;
        _logger.LogInformation("Selected community section: {Section}", section.Name);
    }

    [RelayCommand]
    private void OpenInBrowser(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open URL: {Url}", url);
            _notificationService.ShowError("Failed to open browser");
        }
    }

    [RelayCommand]
    private void JoinDiscord()
    {
        OpenInBrowser("https://discord.gg/savestate");
        _notificationService.ShowInfo("Opening Discord invite...");
    }

    [RelayCommand]
    private void DownloadSaveState(SharedSaveState saveState)
    {
        _logger.LogInformation("Downloading save state: {Title}", saveState.Title);
        _notificationService.ShowInfo($"Downloading '{saveState.Title}'...");
        saveState.Downloads++;
    }

    [RelayCommand]
    private void ViewSaveStateDetails(SharedSaveState saveState)
    {
        _logger.LogDebug("Viewing save state details: {Title}", saveState.Title);
    }

    [RelayCommand]
    private void JoinTournament(TournamentListing tournament)
    {
        _logger.LogInformation("Joining tournament: {Name}", tournament.Name);
        _notificationService.ShowInfo($"Registered for '{tournament.Name}'!");
    }

    [RelayCommand]
    private void ViewTournamentDetails(TournamentListing tournament)
    {
        _logger.LogDebug("Viewing tournament details: {Name}", tournament.Name);
    }

    [RelayCommand]
    private void DownloadContent(UserContent content)
    {
        _logger.LogInformation("Downloading content: {Title}", content.Title);
        _notificationService.ShowInfo($"Downloading '{content.Title}'...");
        content.Downloads++;
    }

    [RelayCommand]
    private void ViewContentDetails(UserContent content)
    {
        _logger.LogDebug("Viewing content details: {Title}", content.Title);
    }

    [RelayCommand]
    private void SearchCommunity()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        _logger.LogInformation("Searching community for: {Query}", SearchQuery);
        _notificationService.ShowInfo($"Searching for '{SearchQuery}'...");
    }

    [RelayCommand]
    private void RefreshContent()
    {
        IsLoading = true;

        // Simulate loading
        System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ =>
        {
            IsLoading = false;
            _notificationService.ShowInfo("Content refreshed");
        });
    }

    [RelayCommand]
    private void ShareSaveState()
    {
        _logger.LogInformation("Opening share save state dialog");
        _notificationService.ShowInfo("Share dialog opened");
    }

    [RelayCommand]
    private void UploadContent()
    {
        _logger.LogInformation("Opening upload content dialog");
        _notificationService.ShowInfo("Upload dialog opened");
    }

    [RelayCommand]
    private void ViewPost(CommunityPost post)
    {
        _logger.LogDebug("Viewing post: {Title}", post.Title);
    }

    [RelayCommand]
    private void LikeContent(object content)
    {
        _notificationService.ShowInfo("Liked!");
    }
}

/// <summary>
/// Represents a community section/link.
/// </summary>
public class CommunitySection
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "🔗";
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a community forum post.
/// </summary>
public class CommunityPost
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Replies { get; set; }
    public int Views { get; set; }
    public DateTime PostedAt { get; set; }
    public string Category { get; set; } = string.Empty;

    public string TimeAgo => FormatTimeAgo(PostedAt);

    private static string FormatTimeAgo(DateTime dateTime)
    {
        var span = DateTime.Now - dateTime;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return dateTime.ToString("MMM dd");
    }
}

/// <summary>
/// Represents a tournament listing.
/// </summary>
public class TournamentListing
{
    public string Name { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public string Prize { get; set; } = string.Empty;
    public int Participants { get; set; }
    public int MaxParticipants { get; set; }
    public TournamentStatus Status { get; set; }

    public string TimeUntil => FormatTimeUntil(StartDate);
    public double Progress => (double)Participants / MaxParticipants * 100;

    private static string FormatTimeUntil(DateTime dateTime)
    {
        var span = dateTime - DateTime.Now;
        if (span.TotalDays < 1) return $"Starts in {(int)span.TotalHours}h";
        return $"Starts in {(int)span.TotalDays}d";
    }
}

public enum TournamentStatus
{
    Open,
    AlmostFull,
    Full,
    InProgress,
    Completed
}

/// <summary>
/// Represents a shared save state.
/// </summary>
public class SharedSaveState
{
    public string Title { get; set; } = string.Empty;
    public string Game { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Downloads { get; set; }
    public double Rating { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// Represents user-generated content.
/// </summary>
public class UserContent
{
    public string Title { get; set; } = string.Empty;
    public ContentType Type { get; set; }
    public string Author { get; set; } = string.Empty;
    public int Downloads { get; set; }
    public double Rating { get; set; }
    public string? PreviewUrl { get; set; }
    public string Description { get; set; } = string.Empty;

    public string TypeIcon => Type switch
    {
        ContentType.Theme => "🎨",
        ContentType.Plugin => "🔌",
        ContentType.Shader => "✨",
        ContentType.Character => "👤",
        ContentType.Stage => "🏟️",
        ContentType.Soundpack => "🎵",
        _ => "📦"
    };
}

public enum ContentType
{
    Theme,
    Plugin,
    Shader,
    Character,
    Stage,
    Soundpack,
    Other
}
