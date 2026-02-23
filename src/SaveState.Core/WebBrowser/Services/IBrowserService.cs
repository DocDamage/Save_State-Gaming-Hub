using SaveState.Core.Common;
using SaveState.Core.WebBrowser.Models;

namespace SaveState.Core.WebBrowser.Services;

public interface IBrowserService
{
    // Lifecycle
    Task<Result> InitializeAsync(BrowserSettings? settings = null);
    Task<Result> ShutdownAsync();
    Task<Result> RestartAsync();
    Task<Result<bool>> IsInitializedAsync();
    bool IsInitialized { get; }
    
    // Tab Management
    Task<Result<BrowserTab>> CreateTabAsync(string? url = null, bool activate = true, bool isIncognito = false);
    Task<Result> CloseTabAsync(Guid tabId);
    Task<Result> CloseAllTabsExceptAsync(Guid tabIdToKeep);
    Task<Result> ActivateTabAsync(Guid tabId);
    Task<Result<BrowserTab>> DuplicateTabAsync(Guid tabId);
    Task<Result> ReorderTabsAsync(List<Guid> tabOrder);
    
    // Tab queries
    Task<Result<IReadOnlyList<BrowserTab>>> GetTabsAsync();
    Task<Result<BrowserTab>> GetTabAsync(Guid tabId);
    Task<Result<BrowserTab?>> GetActiveTabAsync();
    Task<Result> SwitchTabAsync(Guid tabId);
    
    // Tab state
    Task<Result> PinTabAsync(Guid tabId);
    Task<Result> UnpinTabAsync(Guid tabId);
    Task<Result> MuteTabAsync(Guid tabId);
    Task<Result> UnmuteTabAsync(Guid tabId);
    
    IReadOnlyList<BrowserTab> Tabs { get; }
    BrowserTab? ActiveTab { get; }
    event EventHandler<BrowserTab>? TabCreated;
    event EventHandler<BrowserTab>? TabClosed;
    event EventHandler<BrowserTab>? ActiveTabChanged;
    
    // Navigation
    Task<Result> NavigateAsync(Guid tabId, string url);
    Task<Result> NavigateToAsync(Guid tabId, string url);
    Task<Result> GoBackAsync(Guid tabId);
    Task<Result> GoForwardAsync(Guid tabId);
    Task<Result> RefreshAsync(Guid tabId);
    Task<Result> StopAsync(Guid tabId);
    Task<Result> StopLoadingAsync(Guid tabId);
    
    event EventHandler<(Guid TabId, string Url)>? AddressChanged;
    event EventHandler<(Guid TabId, string Title)>? TitleChanged;
    event EventHandler<(Guid TabId, double Progress)>? LoadingProgressChanged;
    event EventHandler<(Guid TabId, bool IsLoading)>? LoadingStateChanged;
    
    // Content
    Task<Result<string>> GetSourceAsync(Guid tabId);
    Task<Result<string>> GetTextAsync(Guid tabId);
    Task<Result> ExecuteScriptAsync(Guid tabId, string script);
    Task<Result<T?>> EvaluateScriptAsync<T>(Guid tabId, string script);
    Task<Result> PrintAsync(Guid tabId);
    Task<Result> PrintToPdfAsync(Guid tabId, string path);
    
    // Zoom
    Task<Result> SetZoomAsync(Guid tabId, ZoomLevel zoom);
    Task<Result> ZoomInAsync(Guid tabId);
    Task<Result> ZoomOutAsync(Guid tabId);
    Task<Result> ResetZoomAsync(Guid tabId);
    
    // Bookmarks
    Task<Result> AddBookmarkAsync(string title, string url, string? folder = null);
    Task<Result<BrowserBookmark>> AddBookmarkAsync(BrowserBookmark bookmark);
    Task<Result> RemoveBookmarkAsync(Guid bookmarkId);
    Task<Result> DeleteBookmarkAsync(Guid bookmarkId);
    Task<Result> UpdateBookmarkAsync(BrowserBookmark bookmark);
    Task<Result<IReadOnlyList<BrowserBookmark>>> GetBookmarksAsync(string? folder = null);
    Task<Result<IReadOnlyList<string>>> GetBookmarkFoldersAsync();
    Task<Result> ImportBookmarksAsync(string htmlContent);
    Task<Result<string>> ExportBookmarksAsync();
    
    // History
    Task<Result> AddHistoryItemAsync(string title, string url);
    Task<Result<BrowserHistoryItem>> AddToHistoryAsync(HistoryItem item);
    Task<Result<IReadOnlyList<BrowserHistoryItem>>> GetHistoryAsync(DateTime? from = null, DateTime? to = null);
    Task<Result> ClearHistoryAsync();
    Task<Result> DeleteHistoryItemAsync(Guid historyId);
    Task<Result<IReadOnlyList<BrowserHistoryItem>>> SearchHistoryAsync(string query);
    
    // Downloads
    IReadOnlyList<BrowserDownload> Downloads { get; }
    Task<Result<IReadOnlyList<BrowserDownload>>> GetDownloadsAsync();
    Task<Result> CancelDownloadAsync(Guid downloadId);
    Task<Result> PauseDownloadAsync(Guid downloadId);
    Task<Result> ResumeDownloadAsync(Guid downloadId);
    Task<Result> ClearCompletedDownloadsAsync();
    Task<Result<DownloadSettings>> GetDownloadSettingsAsync();
    Task<Result> UpdateDownloadSettingsAsync(DownloadSettings settings);
    event EventHandler<BrowserDownload>? DownloadStarted;
    event EventHandler<BrowserDownload>? DownloadProgressChanged;
    event EventHandler<BrowserDownload>? DownloadCompleted;
    
    // Find
    Task<Result> FindAsync(Guid tabId, BrowserFindOptions options);
    Task<Result> FindInPageAsync(Guid tabId, BrowserFindOptions options);
    Task<Result> FindNextAsync(Guid tabId);
    Task<Result> FindPreviousAsync(Guid tabId);
    Task<Result> StopFindingAsync(Guid tabId, bool clearSelection = false);
    
    // DevTools
    Task<Result> ShowDevToolsAsync(Guid tabId);
    Task<Result> CloseDevToolsAsync(Guid tabId);
    Task<Result> InspectElementAsync(Guid tabId, int x, int y);
    
    // Screenshots
    Task<Result<byte[]>> CaptureScreenshotAsync(Guid tabId);
    Task<Result> SaveScreenshotAsync(Guid tabId, string path);
    
    // Cookies
    Task<Result<IReadOnlyList<BrowserCookie>>> GetCookiesAsync(string url);
    Task<Result> SetCookieAsync(BrowserCookie cookie);
    Task<Result> ClearCookiesAsync();
    Task<Result> ClearCookiesForDomainAsync(string domain);
    
    // Settings
    Task<Result<BrowserSettings>> GetSettingsAsync();
    Task<Result> UpdateSettingsAsync(BrowserSettings settings);
    Task<Result> SetHomePageAsync(string homePage);
    Task<Result> SetSearchEngineAsync(string searchEngine);
    Task<Result> SetDefaultZoomAsync(ZoomLevel zoom);
    Task<Result> ClearBrowserDataAsync(BrowserDataType dataTypes);
    BrowserSettings CurrentSettings { get; }
    
    // Extensions
    Task<Result<IReadOnlyList<BrowserExtension>>> GetExtensionsAsync();
    Task<Result> LoadExtensionAsync(string extensionPath);
    Task<Result> EnableExtensionAsync(string extensionId);
    Task<Result> DisableExtensionAsync(string extensionId);
    
    // OAuth
    Task<Result<OAuthCallback>> StartOAuthFlowAsync(string provider, string authorizationUrl, string redirectUri, CancellationToken ct = default);
    event EventHandler<OAuthCallback>? OAuthCallbackReceived;
    
    // Context Menu
    event EventHandler<(Guid TabId, List<BrowserContextMenuItem> MenuItems)>? ContextMenuRequested;
}
