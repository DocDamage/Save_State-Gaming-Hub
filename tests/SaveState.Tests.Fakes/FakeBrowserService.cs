using SaveState.Core.Common;
using SaveState.Core.WebBrowser.Models;
using SaveState.Core.WebBrowser.Services;

namespace SaveState.Tests.Fakes;

/// <summary>
/// Fake implementation of IBrowserService for integration testing.
/// </summary>
public class FakeBrowserService : IBrowserService
{
    private readonly List<BrowserTab> _tabs = new();
    private readonly List<BrowserBookmark> _bookmarks = new();
    private readonly List<BrowserHistoryItem> _history = new();
    private readonly List<BrowserDownload> _downloads = new();
    private readonly List<BrowserCookie> _cookies = new();
    private readonly List<BrowserExtension> _extensions = new();
    private BrowserSettings _settings = new();
    private DownloadSettings _downloadSettings = new();

    public bool IsInitialized { get; private set; }
    public IReadOnlyList<BrowserTab> Tabs => _tabs;
    public BrowserTab? ActiveTab { get; private set; }
    public IReadOnlyList<BrowserDownload> Downloads => _downloads;
    public BrowserSettings CurrentSettings => _settings;

    public event EventHandler<BrowserTab>? TabCreated;
    public event EventHandler<BrowserTab>? TabClosed;
    public event EventHandler<BrowserTab>? ActiveTabChanged;
    public event EventHandler<(Guid TabId, string Url)>? AddressChanged;
    public event EventHandler<(Guid TabId, string Title)>? TitleChanged;
    public event EventHandler<(Guid TabId, double Progress)>? LoadingProgressChanged;
    public event EventHandler<(Guid TabId, bool IsLoading)>? LoadingStateChanged;
    public event EventHandler<BrowserDownload>? DownloadStarted;
    public event EventHandler<BrowserDownload>? DownloadProgressChanged;
    public event EventHandler<BrowserDownload>? DownloadCompleted;
    public event EventHandler<OAuthCallback>? OAuthCallbackReceived;
    public event EventHandler<(Guid TabId, List<BrowserContextMenuItem> MenuItems)>? ContextMenuRequested;

    public Task<Result> InitializeAsync(BrowserSettings? settings = null)
    {
        _settings = settings ?? new BrowserSettings();
        IsInitialized = true;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ShutdownAsync()
    {
        IsInitialized = false;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> RestartAsync()
    {
        IsInitialized = false;
        IsInitialized = true;
        return Task.FromResult(Result.Success());
    }

    public Task<Result<bool>> IsInitializedAsync()
    {
        return Task.FromResult(Result<bool>.Success(IsInitialized));
    }

    public Task<Result<BrowserTab>> CreateTabAsync(string? url = null, bool activate = true, bool isIncognito = false)
    {
        var tab = new BrowserTab
        {
            Id = Guid.NewGuid(),
            Title = url ?? "about:blank",
            Url = url ?? "about:blank",
            State = BrowserTabState.Loaded,
            CanGoBack = false,
            CanGoForward = false,
            IsLoading = false,
            LoadingProgress = 100,
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            IsMuted = false,
            IsPinned = false,
            IsIncognito = isIncognito,
            Zoom = ZoomLevel.Default
        };

        _tabs.Add(tab);
        TabCreated?.Invoke(this, tab);

        if (activate)
        {
            ActiveTab = tab;
        }

        return Task.FromResult(Result<BrowserTab>.Success(tab));
    }

    public Task<Result> CloseTabAsync(Guid tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            _tabs.Remove(tab);
            TabClosed?.Invoke(this, tab);
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> CloseAllTabsExceptAsync(Guid tabIdToKeep)
    {
        var tabsToClose = _tabs.Where(t => t.Id != tabIdToKeep).ToList();
        foreach (var tab in tabsToClose)
        {
            _tabs.Remove(tab);
            TabClosed?.Invoke(this, tab);
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ActivateTabAsync(Guid tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            ActiveTab = tab;
            tab.LastActiveAt = DateTime.UtcNow;
            ActiveTabChanged?.Invoke(this, tab);
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result<BrowserTab>> DuplicateTabAsync(Guid tabId)
    {
        var existingTab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (existingTab == null)
        {
            return Task.FromResult(Result<BrowserTab>.Failure("Tab not found", ErrorType.NotFound));
        }

        return CreateTabAsync(existingTab.Url);
    }

    public Task<Result> ReorderTabsAsync(List<Guid> tabOrder)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<BrowserTab>>> GetTabsAsync()
    {
        return Task.FromResult(Result<IReadOnlyList<BrowserTab>>.Success(_tabs.ToList()));
    }

    public Task<Result<BrowserTab>> GetTabAsync(Guid tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null)
        {
            return Task.FromResult(Result<BrowserTab>.Failure("Tab not found", ErrorType.NotFound));
        }
        return Task.FromResult(Result<BrowserTab>.Success(tab));
    }

    public Task<Result<BrowserTab?>> GetActiveTabAsync()
    {
        return Task.FromResult(Result<BrowserTab?>.Success(ActiveTab));
    }

    public Task<Result> SwitchTabAsync(Guid tabId)
    {
        return ActivateTabAsync(tabId);
    }

    public Task<Result> PinTabAsync(Guid tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            tab.IsPinned = true;
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UnpinTabAsync(Guid tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            tab.IsPinned = false;
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> MuteTabAsync(Guid tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            tab.IsMuted = true;
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UnmuteTabAsync(Guid tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            tab.IsMuted = false;
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> NavigateAsync(Guid tabId, string url)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            tab.Url = url;
            tab.Title = url;
            AddressChanged?.Invoke(this, (tabId, url));
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> NavigateToAsync(Guid tabId, string url)
    {
        return NavigateAsync(tabId, url);
    }

    public Task<Result> GoBackAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> GoForwardAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> RefreshAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopLoadingAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<string>> GetSourceAsync(Guid tabId)
    {
        return Task.FromResult(Result<string>.Success("<html></html>"));
    }

    public Task<Result<string>> GetTextAsync(Guid tabId)
    {
        return Task.FromResult(Result<string>.Success(""));
    }

    public Task<Result> ExecuteScriptAsync(Guid tabId, string script)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<T?>> EvaluateScriptAsync<T>(Guid tabId, string script)
    {
        return Task.FromResult(Result<T?>.Success(default));
    }

    public Task<Result> PrintAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> PrintToPdfAsync(Guid tabId, string path)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SetZoomAsync(Guid tabId, ZoomLevel zoom)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            tab.Zoom = zoom;
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ZoomInAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ZoomOutAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ResetZoomAsync(Guid tabId)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null)
        {
            tab.Zoom = ZoomLevel.Default;
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> AddBookmarkAsync(string title, string url, string? folder = null)
    {
        var bookmark = new BrowserBookmark
        {
            Id = Guid.NewGuid(),
            Title = title,
            Url = url,
            Folder = folder,
            CreatedAt = DateTime.UtcNow
        };
        _bookmarks.Add(bookmark);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<BrowserBookmark>> AddBookmarkAsync(BrowserBookmark bookmark)
    {
        bookmark.Id = Guid.NewGuid();
        bookmark.CreatedAt = DateTime.UtcNow;
        _bookmarks.Add(bookmark);
        return Task.FromResult(Result<BrowserBookmark>.Success(bookmark));
    }

    public Task<Result> RemoveBookmarkAsync(Guid bookmarkId)
    {
        var bookmark = _bookmarks.FirstOrDefault(b => b.Id == bookmarkId);
        if (bookmark != null)
        {
            _bookmarks.Remove(bookmark);
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteBookmarkAsync(Guid bookmarkId)
    {
        return RemoveBookmarkAsync(bookmarkId);
    }

    public Task<Result> UpdateBookmarkAsync(BrowserBookmark bookmark)
    {
        var existing = _bookmarks.FirstOrDefault(b => b.Id == bookmark.Id);
        if (existing != null)
        {
            _bookmarks.Remove(existing);
            _bookmarks.Add(bookmark);
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<BrowserBookmark>>> GetBookmarksAsync(string? folder = null)
    {
        var bookmarks = string.IsNullOrEmpty(folder)
            ? _bookmarks
            : _bookmarks.Where(b => b.Folder == folder).ToList();
        return Task.FromResult(Result<IReadOnlyList<BrowserBookmark>>.Success(bookmarks));
    }

    public Task<Result<IReadOnlyList<string>>> GetBookmarkFoldersAsync()
    {
        var folders = _bookmarks.Select(b => b.Folder).Where(f => !string.IsNullOrEmpty(f)).Distinct().ToList()!;
        return Task.FromResult(Result<IReadOnlyList<string>>.Success(folders));
    }

    public Task<Result> ImportBookmarksAsync(string htmlContent)
    {
        // Simple fake implementation - just add a test bookmark
        _bookmarks.Add(new BrowserBookmark
        {
            Id = Guid.NewGuid(),
            Title = "Imported Bookmark",
            Url = "https://example.com/imported",
            CreatedAt = DateTime.UtcNow
        });
        return Task.FromResult(Result.Success());
    }

    public Task<Result<string>> ExportBookmarksAsync()
    {
        const string html = @"<!DOCTYPE NETSCAPE-Bookmark-file-1>
<html>
<head><title>Bookmarks</title></head>
<body>
<h1>Bookmarks</h1>
</body>
</html>";
        return Task.FromResult(Result<string>.Success(html));
    }

    public Task<Result> AddHistoryItemAsync(string title, string url)
    {
        var item = new BrowserHistoryItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Url = url,
            VisitedAt = DateTime.UtcNow
        };
        _history.Add(item);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<BrowserHistoryItem>> AddToHistoryAsync(HistoryItem item)
    {
        var historyItem = new BrowserHistoryItem
        {
            Id = Guid.NewGuid(),
            Title = item.Title,
            Url = item.Url,
            VisitedAt = item.VisitedAt
        };
        _history.Add(historyItem);
        return Task.FromResult(Result<BrowserHistoryItem>.Success(historyItem));
    }

    public Task<Result<IReadOnlyList<BrowserHistoryItem>>> GetHistoryAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _history.AsEnumerable();
        if (from.HasValue)
            query = query.Where(h => h.VisitedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(h => h.VisitedAt <= to.Value);

        return Task.FromResult(Result<IReadOnlyList<BrowserHistoryItem>>.Success(query.ToList()));
    }

    public Task<Result<IReadOnlyList<BrowserHistoryItem>>> SearchHistoryAsync(string query)
    {
        var results = _history.Where(h => 
            h.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || 
            h.Url.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        return Task.FromResult(Result<IReadOnlyList<BrowserHistoryItem>>.Success(results));
    }

    public Task<Result> ClearHistoryAsync()
    {
        _history.Clear();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteHistoryItemAsync(Guid historyId)
    {
        var item = _history.FirstOrDefault(h => h.Id == historyId);
        if (item != null)
        {
            _history.Remove(item);
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<BrowserDownload>>> GetDownloadsAsync()
    {
        return Task.FromResult(Result<IReadOnlyList<BrowserDownload>>.Success(_downloads.ToList()));
    }

    public Task<Result> CancelDownloadAsync(Guid downloadId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> PauseDownloadAsync(Guid downloadId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ResumeDownloadAsync(Guid downloadId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ClearCompletedDownloadsAsync()
    {
        _downloads.RemoveAll(d => d.State == DownloadState.Completed);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<DownloadSettings>> GetDownloadSettingsAsync()
    {
        return Task.FromResult(Result<DownloadSettings>.Success(_downloadSettings));
    }

    public Task<Result> UpdateDownloadSettingsAsync(DownloadSettings settings)
    {
        _downloadSettings = settings;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> FindAsync(Guid tabId, BrowserFindOptions options)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> FindInPageAsync(Guid tabId, BrowserFindOptions options)
    {
        return FindAsync(tabId, options);
    }

    public Task<Result> FindNextAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> FindPreviousAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopFindingAsync(Guid tabId, bool clearSelection = false)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<BrowserCookie>>> GetCookiesAsync(string url)
    {
        var urlCookies = _cookies.Where(c => url.Contains(c.Domain)).ToList();
        return Task.FromResult(Result<IReadOnlyList<BrowserCookie>>.Success(urlCookies));
    }

    public Task<Result> SetCookieAsync(BrowserCookie cookie)
    {
        _cookies.Add(cookie);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ClearCookiesAsync()
    {
        _cookies.Clear();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ClearCookiesForDomainAsync(string domain)
    {
        _cookies.RemoveAll(c => c.Domain == domain);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<BrowserSettings>> GetSettingsAsync()
    {
        return Task.FromResult(Result<BrowserSettings>.Success(_settings));
    }

    public Task<Result> SetHomePageAsync(string homePage)
    {
        _settings.HomePage = homePage;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SetSearchEngineAsync(string searchEngine)
    {
        _settings.SearchEngine = searchEngine;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SetDefaultZoomAsync(ZoomLevel zoom)
    {
        _settings.DefaultZoom = zoom;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ClearBrowserDataAsync(BrowserDataType dataTypes)
    {
        if (dataTypes.HasFlag(BrowserDataType.Cookies))
            _cookies.Clear();
        if (dataTypes.HasFlag(BrowserDataType.History))
            _history.Clear();
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<BrowserExtension>>> GetExtensionsAsync()
    {
        return Task.FromResult(Result<IReadOnlyList<BrowserExtension>>.Success(_extensions.ToList()));
    }

    public Task<Result> LoadExtensionAsync(string extensionPath)
    {
        _extensions.Add(new BrowserExtension
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Test Extension",
            Path = extensionPath,
            IsLoaded = true
        });
        return Task.FromResult(Result.Success());
    }

    public Task<Result> EnableExtensionAsync(string extensionId)
    {
        var ext = _extensions.FirstOrDefault(e => e.Id == extensionId);
        if (ext != null)
            ext.IsEnabled = true;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DisableExtensionAsync(string extensionId)
    {
        var ext = _extensions.FirstOrDefault(e => e.Id == extensionId);
        if (ext != null)
            ext.IsEnabled = false;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ShowDevToolsAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> CloseDevToolsAsync(Guid tabId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> InspectElementAsync(Guid tabId, int x, int y)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<byte[]>> CaptureScreenshotAsync(Guid tabId)
    {
        return Task.FromResult(Result<byte[]>.Success(Array.Empty<byte>()));
    }

    public Task<Result> SaveScreenshotAsync(Guid tabId, string path)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UpdateSettingsAsync(BrowserSettings settings)
    {
        _settings = settings;
        return Task.FromResult(Result.Success());
    }

    public Task<Result<OAuthCallback>> StartOAuthFlowAsync(string provider, string authorizationUrl, string redirectUri, CancellationToken ct = default)
    {
        var callback = new OAuthCallback
        {
            Provider = provider,
            Code = "test_code",
            State = "test_state"
        };
        return Task.FromResult(Result<OAuthCallback>.Success(callback));
    }
}
