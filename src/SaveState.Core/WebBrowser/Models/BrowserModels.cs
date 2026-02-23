namespace SaveState.Core.WebBrowser.Models;

public enum BrowserTabState
{
    Loading,
    Loaded,
    Error,
    Crashed
}

public enum BrowserNavigationAction
{
    Back,
    Forward,
    Refresh,
    Stop,
    Home
}

public record BrowserTab
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "New Tab";
    public string Url { get; set; } = "about:blank";
    public string? Favicon { get; set; }
    public BrowserTabState State { get; set; } = BrowserTabState.Loading;
    public bool CanGoBack { get; set; }
    public bool CanGoForward { get; set; }
    public bool IsLoading { get; set; }
    public double LoadingProgress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastActiveAt { get; set; }
    public bool IsMuted { get; set; }
    public bool IsPinned { get; set; }
    public bool IsIncognito { get; set; }
    public ZoomLevel Zoom { get; set; } = ZoomLevel.Default;
}

public enum ZoomLevel
{
    Minimum = -10,  // 25%
    Far = -5,       // 50%
    Default = 0,    // 100%
    Close = 5,      // 200%
    Maximum = 10    // 500%
}

public record BrowserBookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Favicon { get; set; }
    public string? Folder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int VisitCount { get; set; }
    public DateTime? LastVisited { get; set; }
}

public record BrowserHistoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Favicon { get; set; }
    public DateTime VisitedAt { get; set; } = DateTime.Now;
    public int VisitCount { get; set; } = 1;
}

public record BrowserDownload
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long TotalBytes { get; set; }
    public long ReceivedBytes { get; set; }
    public double Progress => TotalBytes > 0 ? (double)ReceivedBytes / TotalBytes : 0;
    public DownloadState State { get; set; } = DownloadState.InProgress;
    public string? SavePath { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
}

public enum DownloadState
{
    InProgress,
    Completed,
    Canceled,
    Failed
}

public record BrowserSettings
{
    public string HomePage { get; set; } = "https://www.google.com";
    public string SearchEngine { get; set; } = "https://www.google.com/search?q=";
    public bool EnableJavaScript { get; set; } = true;
    public bool EnablePlugins { get; set; } = true;
    public bool EnableWebSecurity { get; set; } = true;
    public bool EnableDownloads { get; set; } = true;
    public string DownloadPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public bool ClearDataOnExit { get; set; } = false;
    public bool DoNotTrack { get; set; } = true;
    public bool BlockPopups { get; set; } = true;
    public List<string> BlockedDomains { get; set; } = new();
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
    public string? ProxyAddress { get; set; }
    public int? ProxyPort { get; set; }
    public bool IsIncognito { get; set; } = false;
    public ZoomLevel DefaultZoom { get; set; } = ZoomLevel.Default;
}

public record BrowserContextMenuItem
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Command { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsSeparator { get; set; }
    public List<BrowserContextMenuItem>? SubItems { get; set; }
}

public record BrowserFindOptions
{
    public string SearchText { get; set; } = string.Empty;
    public bool Forward { get; set; } = true;
    public bool MatchCase { get; set; } = false;
    public bool FindNext { get; set; } = false;
}

public record OAuthCallback
{
    public string Provider { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? State { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}

public record HistoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
}

public record DownloadSettings
{
    public string DownloadPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public bool EnableDownloads { get; set; } = true;
    public int MaxConcurrentDownloads { get; set; } = 3;
    public bool AskBeforeDownload { get; set; } = true;
}

public record BrowserCookie
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Path { get; set; } = "/";
    public DateTime? Expires { get; set; }
    public bool IsSecure { get; set; }
    public bool IsHttpOnly { get; set; }
    public string? SameSite { get; set; }
}

public record BrowserExtension
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public string? IconPath { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsLoaded { get; set; }
    public string? Path { get; set; }
}

[Flags]
public enum BrowserDataType
{
    None = 0,
    Cache = 1,
    Cookies = 2,
    History = 4,
    FormData = 8,
    Passwords = 16,
    LocalStorage = 32,
    SessionStorage = 64,
    All = Cache | Cookies | History | FormData | Passwords | LocalStorage | SessionStorage
}
