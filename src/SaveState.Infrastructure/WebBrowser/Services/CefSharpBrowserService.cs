using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.WebBrowser.Models;
using SaveState.Core.WebBrowser.Services;
using SaveState.Infrastructure.WebBrowser.Handlers;
using CefSharp;
using CefSharp.OffScreen;
using BrowserSettingsModel = SaveState.Core.WebBrowser.Models.BrowserSettings;
using CefBrowserSettings = CefSharp.BrowserSettings;

namespace SaveState.Infrastructure.WebBrowser.Services;

/// <summary>
/// CefSharp-based browser service implementation for SaveStateReborn.
/// Provides tabbed browsing, downloads, OAuth handling, and DevTools integration.
/// </summary>
public sealed class CefSharpBrowserService : IBrowserService, IDisposable
{
    private readonly ILogger<CefSharpBrowserService> _logger;
    private readonly ConcurrentDictionary<Guid, BrowserTabInstance> _tabs = new();
    private readonly ConcurrentDictionary<Guid, BrowserDownload> _downloads = new();
    private readonly List<BrowserBookmark> _bookmarks = new();
    private readonly List<BrowserHistoryItem> _history = new();
    private readonly object _historyLock = new();
    private readonly object _bookmarkLock = new();
    
    private BrowserSettingsModel _settings = new();
    private bool _isInitialized;
    private TaskCompletionSource<OAuthCallback>? _oauthTcs;
    private Guid? _oauthTabId;

    public bool IsInitialized => _isInitialized;
    public IReadOnlyList<BrowserTab> Tabs => _tabs.Values.Select(t => t.Model).ToList();
    public IReadOnlyList<BrowserDownload> Downloads => _downloads.Values.ToList();
    public BrowserTab? ActiveTab => _tabs.Values.FirstOrDefault(t => t.IsActive)?.Model;
    public BrowserSettingsModel CurrentSettings => _settings;

    // Events
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

    public CefSharpBrowserService(ILogger<CefSharpBrowserService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result> InitializeAsync(BrowserSettingsModel? settings = null)
    {
        if (_isInitialized)
            return Task.FromResult(Result.Success());

        try
        {
            _settings = settings ?? new BrowserSettingsModel();
            
            var cefSettings = new CefSettings
            {
                CachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SaveState", "BrowserCache"),
                UserAgent = $"SaveStateReborn/2.5.2 (Windows NT 10.0; Win64; x64)",
                AcceptLanguageList = "en-US,en",
                MultiThreadedMessageLoop = true,
                ExternalMessagePump = false,
                WindowlessRenderingEnabled = true
            };

            // Configure proxy if set
            if (!string.IsNullOrEmpty(_settings.ProxyAddress) && _settings.ProxyPort.HasValue)
            {
                cefSettings.CefCommandLineArgs.Add("proxy-server", $"{_settings.ProxyAddress}:{_settings.ProxyPort.Value}");
            }

            // Disable web security for local file access if needed
            if (!_settings.EnableWebSecurity)
            {
                cefSettings.CefCommandLineArgs.Add("disable-web-security", "1");
            }

            Cef.Initialize(cefSettings, performDependencyCheck: true, browserProcessHandler: null);
            
            _isInitialized = true;
            _logger.LogInformation("CefSharp browser service initialized successfully");
            
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize CefSharp browser service");
            return Task.FromResult(Result.Failure($"Failed to initialize browser: {ex.Message}"));
        }
    }

    public Task<Result> ShutdownAsync()
    {
        if (!_isInitialized)
            return Task.FromResult(Result.Success());

        try
        {
            // Close all tabs
            foreach (var tab in _tabs.Values)
            {
                tab.Browser?.Dispose();
            }
            _tabs.Clear();
            _downloads.Clear();

            if (_settings.ClearDataOnExit)
            {
                ClearBrowserData();
            }

            Cef.Shutdown();
            _isInitialized = false;
            
            _logger.LogInformation("CefSharp browser service shut down successfully");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during browser shutdown");
            return Task.FromResult(Result.Failure($"Shutdown error: {ex.Message}"));
        }
    }

    public async Task<Result> RestartAsync()
    {
        await ShutdownAsync();
        return await InitializeAsync(_settings);
    }

    public Task<Result<bool>> IsInitializedAsync()
    {
        return Task.FromResult(Result<bool>.Success(_isInitialized));
    }

    public Task<Result<BrowserTab>> CreateTabAsync(string? url = null, bool activate = true, bool isIncognito = false)
    {
        if (!_isInitialized)
            return Task.FromResult(Result<BrowserTab>.Failure("Browser not initialized"));

        try
        {
            var tabId = Guid.NewGuid();
            // Create browser settings - note: Plugins property removed in newer CefSharp versions
            var browserSettings = new CefBrowserSettings
            {
                Javascript = _settings.EnableJavaScript ? CefState.Enabled : CefState.Disabled
            };

            // Create request context settings - for incognito mode, don't set cache path
            var requestContextSettings = new RequestContextSettings();
            if (!isIncognito)
            {
                requestContextSettings.CachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SaveState", "BrowserCache");
            }

            var requestContext = new RequestContext(requestContextSettings);
            
            // Create handlers
            var downloadHandler = new CustomDownloadHandler(_logger, OnDownloadStarted, OnDownloadProgress, OnDownloadCompleted);
            var lifeSpanHandler = new CustomLifeSpanHandler(_logger, OnPopup, OnBeforePopupClose);
            var displayHandler = new CustomDisplayHandler(_logger, OnAddressChanged, OnTitleChanged, OnLoadingProgress, OnLoadingStateChanged);
            var contextMenuHandler = new CustomContextMenuHandler(_logger, OnContextMenuRequested);
            var keyboardHandler = new CustomKeyboardHandler(_logger, OnKeyEvent);
            var requestHandler = new CustomRequestHandler(_logger, _settings.BlockedDomains, _settings.CustomHeaders);
            var jsDialogHandler = new CustomJsDialogHandler(_logger);

            // Create browser with settings and request context via constructor
            var browser = new ChromiumWebBrowser(
                address: url ?? "about:blank",
                browserSettings: browserSettings,
                requestContext: requestContext)
            {
                DownloadHandler = downloadHandler,
                LifeSpanHandler = lifeSpanHandler,
                DisplayHandler = displayHandler,
                MenuHandler = contextMenuHandler,
                KeyboardHandler = keyboardHandler,
                RequestHandler = requestHandler,
                JsDialogHandler = jsDialogHandler
            };

            var model = new BrowserTab
            {
                Id = tabId,
                Url = url ?? "about:blank",
                IsLoading = !string.IsNullOrEmpty(url),
                State = BrowserTabState.Loading
            };

            var instance = new BrowserTabInstance
            {
                Model = model,
                Browser = browser,
                DownloadHandler = downloadHandler
            };

            _tabs[tabId] = instance;

            if (activate)
            {
                SetActiveTab(tabId);
            }

            TabCreated?.Invoke(this, model);
            
            _logger.LogDebug("Created new tab {TabId} with URL {Url}", tabId, url);
            return Task.FromResult(Result<BrowserTab>.Success(model));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new tab");
            return Task.FromResult(Result<BrowserTab>.Failure($"Failed to create tab: {ex.Message}"));
        }
    }

    public Task<Result> CloseTabAsync(Guid tabId)
    {
        if (!_tabs.TryRemove(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        try
        {
            tab.Browser?.Dispose();
            TabClosed?.Invoke(this, tab.Model);
            
            _logger.LogDebug("Closed tab {TabId}", tabId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing tab {TabId}", tabId);
            return Task.FromResult(Result.Failure($"Error closing tab: {ex.Message}"));
        }
    }

    public Task<Result> CloseAllTabsExceptAsync(Guid tabIdToKeep)
    {
        var tabsToClose = _tabs.Values.Where(t => t.Model.Id != tabIdToKeep).ToList();
        foreach (var tab in tabsToClose)
        {
            _tabs.TryRemove(tab.Model.Id, out _);
            tab.Browser?.Dispose();
            TabClosed?.Invoke(this, tab.Model);
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ActivateTabAsync(Guid tabId)
    {
        if (!_tabs.ContainsKey(tabId))
            return Task.FromResult(Result.Failure("Tab not found"));

        SetActiveTab(tabId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<BrowserTab>> DuplicateTabAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var sourceTab))
            return Task.FromResult(Result<BrowserTab>.Failure("Source tab not found"));

        return CreateTabAsync(sourceTab.Model.Url, activate: true);
    }

    public Task<Result> ReorderTabsAsync(List<Guid> tabOrder)
    {
        // Tabs are stored in dictionary, ordering is handled by UI
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<BrowserTab>>> GetTabsAsync()
    {
        return Task.FromResult(Result<IReadOnlyList<BrowserTab>>.Success(
            _tabs.Values.Select(t => t.Model).ToList()));
    }

    public Task<Result<BrowserTab>> GetTabAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result<BrowserTab>.Failure("Tab not found"));
        return Task.FromResult(Result<BrowserTab>.Success(tab.Model));
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
        if (_tabs.TryGetValue(tabId, out var tab))
        {
            tab.Model = tab.Model with { IsPinned = true };
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UnpinTabAsync(Guid tabId)
    {
        if (_tabs.TryGetValue(tabId, out var tab))
        {
            tab.Model = tab.Model with { IsPinned = false };
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> MuteTabAsync(Guid tabId)
    {
        if (_tabs.TryGetValue(tabId, out var tab))
        {
            tab.Model = tab.Model with { IsMuted = true };
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UnmuteTabAsync(Guid tabId)
    {
        if (_tabs.TryGetValue(tabId, out var tab))
        {
            tab.Model = tab.Model with { IsMuted = false };
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> NavigateAsync(Guid tabId, string url)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        try
        {
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && 
                !url.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            {
                url = _settings.SearchEngine + Uri.EscapeDataString(url);
            }

            tab.Browser.Load(url);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to {Url}", url);
            return Task.FromResult(Result.Failure($"Navigation error: {ex.Message}"));
        }
    }

    public Task<Result> NavigateToAsync(Guid tabId, string url)
    {
        return NavigateAsync(tabId, url);
    }

    public Task<Result> GoBackAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        tab.Browser.Back();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> GoForwardAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        tab.Browser.Forward();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> RefreshAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        tab.Browser.Reload();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        tab.Browser.Stop();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopLoadingAsync(Guid tabId)
    {
        return StopAsync(tabId);
    }

    public async Task<Result<string>> GetSourceAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Result<string>.Failure("Tab not found");

        try
        {
            var source = await tab.Browser.GetSourceAsync();
            return Result<string>.Success(source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting source for tab {TabId}", tabId);
            return Result<string>.Failure($"Error getting source: {ex.Message}");
        }
    }

    public async Task<Result<string>> GetTextAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Result<string>.Failure("Tab not found");

        try
        {
            var text = await tab.Browser.GetTextAsync();
            return Result<string>.Success(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting text for tab {TabId}", tabId);
            return Result<string>.Failure($"Error getting text: {ex.Message}");
        }
    }

    public async Task<Result> ExecuteScriptAsync(Guid tabId, string script)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Result.Failure("Tab not found");

        try
        {
            await tab.Browser.EvaluateScriptAsync(script);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing script in tab {TabId}", tabId);
            return Result.Failure($"Script execution error: {ex.Message}");
        }
    }

    public async Task<Result<T?>> EvaluateScriptAsync<T>(Guid tabId, string script)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Result<T?>.Failure("Tab not found");

        try
        {
            var result = await tab.Browser.EvaluateScriptAsync(script);
            
            if (!result.Success)
                return Result<T?>.Failure(result.Message);

            var value = result.Result is T typedResult ? typedResult : default;
            return Result<T?>.Success(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating script in tab {TabId}", tabId);
            return Result<T?>.Failure($"Script evaluation error: {ex.Message}");
        }
    }

    public Task<Result> PrintAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        tab.Browser.Print();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> PrintToPdfAsync(Guid tabId, string path)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        var settings = new PdfPrintSettings
        {
            BackgroundsEnabled = true,
            HeaderFooterEnabled = false,
            MarginType = CefPdfPrintMarginType.Default
        };

        tab.Browser.PrintToPdfAsync(path, settings);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SetZoomAsync(Guid tabId, ZoomLevel zoom)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        tab.Browser.SetZoomLevel((int)zoom);
        tab.Model = tab.Model with { Zoom = zoom };
        
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ZoomInAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        var newZoom = (ZoomLevel)Math.Min((int)tab.Model.Zoom + 1, (int)ZoomLevel.Maximum);
        return SetZoomAsync(tabId, newZoom);
    }

    public Task<Result> ZoomOutAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        var newZoom = (ZoomLevel)Math.Max((int)tab.Model.Zoom - 1, (int)ZoomLevel.Minimum);
        return SetZoomAsync(tabId, newZoom);
    }

    public Task<Result> ResetZoomAsync(Guid tabId)
    {
        return SetZoomAsync(tabId, ZoomLevel.Default);
    }

    public Task<Result> AddBookmarkAsync(string title, string url, string? folder = null)
    {
        lock (_bookmarkLock)
        {
            var bookmark = new BrowserBookmark
            {
                Title = title,
                Url = url,
                Folder = folder,
                CreatedAt = DateTime.Now
            };
            _bookmarks.Add(bookmark);
        }
        
        return Task.FromResult(Result.Success());
    }

    public Task<Result<BrowserBookmark>> AddBookmarkAsync(BrowserBookmark bookmark)
    {
        lock (_bookmarkLock)
        {
            bookmark.CreatedAt = DateTime.Now;
            _bookmarks.Add(bookmark);
            return Task.FromResult(Result<BrowserBookmark>.Success(bookmark));
        }
    }

    public Task<Result> RemoveBookmarkAsync(Guid bookmarkId)
    {
        lock (_bookmarkLock)
        {
            var bookmark = _bookmarks.FirstOrDefault(b => b.Id == bookmarkId);
            if (bookmark != null)
            {
                _bookmarks.Remove(bookmark);
            }
        }
        
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteBookmarkAsync(Guid bookmarkId)
    {
        return RemoveBookmarkAsync(bookmarkId);
    }

    public Task<Result> UpdateBookmarkAsync(BrowserBookmark bookmark)
    {
        lock (_bookmarkLock)
        {
            var existing = _bookmarks.FirstOrDefault(b => b.Id == bookmark.Id);
            if (existing != null)
            {
                var index = _bookmarks.IndexOf(existing);
                _bookmarks[index] = bookmark;
            }
            return Task.FromResult(Result.Success());
        }
    }

    public Task<Result<IReadOnlyList<BrowserBookmark>>> GetBookmarksAsync(string? folder = null)
    {
        lock (_bookmarkLock)
        {
            var bookmarks = string.IsNullOrEmpty(folder) 
                ? _bookmarks.ToList() 
                : _bookmarks.Where(b => b.Folder == folder).ToList();
            
            return Task.FromResult(Result<IReadOnlyList<BrowserBookmark>>.Success(bookmarks));
        }
    }

    public Task<Result<IReadOnlyList<string>>> GetBookmarkFoldersAsync()
    {
        lock (_bookmarkLock)
        {
            var folders = _bookmarks
                .Where(b => !string.IsNullOrEmpty(b.Folder))
                .Select(b => b.Folder!)
                .Distinct()
                .ToList();
            
            return Task.FromResult(Result<IReadOnlyList<string>>.Success(folders));
        }
    }

    public Task<Result> ImportBookmarksAsync(string htmlContent)
    {
        // Stub implementation - would parse HTML in real implementation
        return Task.FromResult(Result.Success());
    }

    public Task<Result<string>> ExportBookmarksAsync()
    {
        // Stub implementation - would generate HTML in real implementation
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
        lock (_historyLock)
        {
            var existing = _history.FirstOrDefault(h => h.Url == url);
            if (existing != null)
            {
                existing.VisitCount++;
                existing.VisitedAt = DateTime.Now;
            }
            else
            {
                _history.Add(new BrowserHistoryItem
                {
                    Title = title,
                    Url = url,
                    VisitedAt = DateTime.Now
                });
            }
        }
        
        return Task.FromResult(Result.Success());
    }

    public Task<Result<BrowserHistoryItem>> AddToHistoryAsync(HistoryItem item)
    {
        lock (_historyLock)
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
    }

    public Task<Result<IReadOnlyList<BrowserHistoryItem>>> GetHistoryAsync(DateTime? from = null, DateTime? to = null)
    {
        lock (_historyLock)
        {
            var query = _history.AsQueryable();
            
            if (from.HasValue)
                query = query.Where(h => h.VisitedAt >= from.Value);
            
            if (to.HasValue)
                query = query.Where(h => h.VisitedAt <= to.Value);
            
            var result = query.OrderByDescending(h => h.VisitedAt).ToList();
            return Task.FromResult(Result<IReadOnlyList<BrowserHistoryItem>>.Success(result));
        }
    }

    public Task<Result<IReadOnlyList<BrowserHistoryItem>>> SearchHistoryAsync(string query)
    {
        lock (_historyLock)
        {
            var results = _history.Where(h => 
                h.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                h.Url.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            return Task.FromResult(Result<IReadOnlyList<BrowserHistoryItem>>.Success(results));
        }
    }

    public Task<Result> ClearHistoryAsync()
    {
        lock (_historyLock)
        {
            _history.Clear();
        }
        
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteHistoryItemAsync(Guid historyId)
    {
        lock (_historyLock)
        {
            var item = _history.FirstOrDefault(h => h.Id == historyId);
            if (item != null)
            {
                _history.Remove(item);
            }
        }
        
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<BrowserDownload>>> GetDownloadsAsync()
    {
        return Task.FromResult(Result<IReadOnlyList<BrowserDownload>>.Success(
            _downloads.Values.ToList()));
    }

    public Task<Result> CancelDownloadAsync(Guid downloadId)
    {
        if (_downloads.TryGetValue(downloadId, out var download))
        {
            download.State = DownloadState.Canceled;
            // Note: Actual cancellation would require storing download callbacks
        }
        
        return Task.FromResult(Result.Success());
    }

    public Task<Result> PauseDownloadAsync(Guid downloadId)
    {
        // CefSharp doesn't support pausing downloads directly
        return Task.FromResult(Result.Failure("Download pausing not supported"));
    }

    public Task<Result> ResumeDownloadAsync(Guid downloadId)
    {
        // CefSharp doesn't support resuming downloads directly
        return Task.FromResult(Result.Failure("Download resuming not supported"));
    }

    public Task<Result> ClearCompletedDownloadsAsync()
    {
        var completedIds = _downloads
            .Where(d => d.Value.State == DownloadState.Completed || d.Value.State == DownloadState.Canceled)
            .Select(d => d.Key)
            .ToList();

        foreach (var id in completedIds)
        {
            _downloads.TryRemove(id, out _);
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result<DownloadSettings>> GetDownloadSettingsAsync()
    {
        return Task.FromResult(Result<DownloadSettings>.Success(new DownloadSettings 
        { 
            DownloadPath = _settings.DownloadPath,
            EnableDownloads = _settings.EnableDownloads
        }));
    }

    public Task<Result> UpdateDownloadSettingsAsync(DownloadSettings settings)
    {
        _settings.DownloadPath = settings.DownloadPath;
        _settings.EnableDownloads = settings.EnableDownloads;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> FindAsync(Guid tabId, BrowserFindOptions options)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        // Find method signature: Find(int identifier, string searchText, bool forward, bool matchCase, bool findNext)
        // Using 0 as identifier (can be any int to identify the find operation)
        // Find method signature in newer CefSharp: Find(string searchText, bool forward, bool matchCase, bool findNext)
        tab.Browser.Find(options.SearchText, options.Forward, options.MatchCase, options.FindNext);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> FindInPageAsync(Guid tabId, BrowserFindOptions options)
    {
        return FindAsync(tabId, options);
    }

    public Task<Result> FindNextAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        // Re-run find with findNext=true
        tab.Browser.Find(string.Empty, true, false, true);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> FindPreviousAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        // Re-run find with forward=false and findNext=true
        tab.Browser.Find(string.Empty, false, false, true);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopFindingAsync(Guid tabId, bool clearSelection = false)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        tab.Browser.StopFinding(clearSelection);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<BrowserCookie>>> GetCookiesAsync(string url)
    {
        // Stub - would use CefCookieManager in real implementation
        return Task.FromResult(Result<IReadOnlyList<BrowserCookie>>.Success(new List<BrowserCookie>()));
    }

    public Task<Result> SetCookieAsync(BrowserCookie cookie)
    {
        // Stub - would use CefCookieManager in real implementation
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ClearCookiesAsync()
    {
        Cef.GetGlobalCookieManager().DeleteCookies(string.Empty, string.Empty);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ClearCookiesForDomainAsync(string domain)
    {
        Cef.GetGlobalCookieManager().DeleteCookies(domain, string.Empty);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<BrowserSettingsModel>> GetSettingsAsync()
    {
        return Task.FromResult(Result<BrowserSettingsModel>.Success(_settings));
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
        {
            Cef.GetGlobalCookieManager().DeleteCookies(string.Empty, string.Empty);
        }
        // Other data types would be cleared via CefRequestContext
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<BrowserExtension>>> GetExtensionsAsync()
    {
        // Extension support is limited in CefSharp
        return Task.FromResult(Result<IReadOnlyList<BrowserExtension>>.Success(new List<BrowserExtension>()));
    }

    public Task<Result> LoadExtensionAsync(string extensionPath)
    {
        // Extension loading would be implemented via CefRequestContext.LoadExtension
        return Task.FromResult(Result.Success());
    }

    public Task<Result> EnableExtensionAsync(string extensionId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DisableExtensionAsync(string extensionId)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ShowDevToolsAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        tab.Browser.ShowDevTools();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> CloseDevToolsAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        tab.Browser.CloseDevTools();
        return Task.FromResult(Result.Success());
    }

    public Task<Result> InspectElementAsync(Guid tabId, int x, int y)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Task.FromResult(Result.Failure("Tab not found"));

        tab.Browser.ShowDevTools();
        // Note: DevTools element inspection would require additional implementation
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<byte[]>> CaptureScreenshotAsync(Guid tabId)
    {
        if (!_tabs.TryGetValue(tabId, out var tab))
            return Result<byte[]>.Failure("Tab not found");

        try
        {
            // CaptureScreenshotAsync returns a byte array directly (PNG format)
            var bitmap = await tab.Browser.CaptureScreenshotAsync();
            return Result<byte[]>.Success(bitmap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing screenshot for tab {TabId}", tabId);
            return Result<byte[]>.Failure($"Screenshot error: {ex.Message}");
        }
    }

    public async Task<Result> SaveScreenshotAsync(Guid tabId, string path)
    {
        var result = await CaptureScreenshotAsync(tabId);
        
        if (result.IsFailure)
            return Result.Failure(result.Error!);

        try
        {
            await File.WriteAllBytesAsync(path, result.Value!);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving screenshot to {Path}", path);
            return Result.Failure($"Save screenshot error: {ex.Message}");
        }
    }

    public Task<Result> UpdateSettingsAsync(BrowserSettingsModel settings)
    {
        _settings = settings;
        // Apply settings to existing tabs would require recreating browsers
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<OAuthCallback>> StartOAuthFlowAsync(
        string provider, 
        string authorizationUrl, 
        string redirectUri, 
        CancellationToken ct = default)
    {
        _oauthTcs = new TaskCompletionSource<OAuthCallback>();
        
        var tabResult = await CreateTabAsync(authorizationUrl, activate: true);
        if (tabResult.IsFailure)
            return Result<OAuthCallback>.Failure(tabResult.Error!);

        _oauthTabId = tabResult.Value!.Id;

        using (ct.Register(() => _oauthTcs.TrySetCanceled()))
        {
            try
            {
                var callback = await _oauthTcs.Task;
                
                // Clean up OAuth tab
                if (_oauthTabId.HasValue)
                {
                    await CloseTabAsync(_oauthTabId.Value);
                }
                
                return Result<OAuthCallback>.Success(callback);
            }
            catch (OperationCanceledException)
            {
                if (_oauthTabId.HasValue)
                {
                    await CloseTabAsync(_oauthTabId.Value);
                }
                return Result<OAuthCallback>.Failure("OAuth flow cancelled", ErrorType.Cancelled);
            }
        }
    }

    public void Dispose()
    {
        _ = ShutdownAsync();
    }

    // Private helper methods
    private void SetActiveTab(Guid tabId)
    {
        foreach (var tab in _tabs.Values)
        {
            tab.IsActive = tab.Model.Id == tabId;
            if (tab.IsActive)
            {
                tab.Model.LastActiveAt = DateTime.Now;
                ActiveTabChanged?.Invoke(this, tab.Model);
            }
        }
    }

    private void ClearBrowserData()
    {
        // Clear cache, cookies, etc.
        var cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveState", "BrowserCache");
        
        if (Directory.Exists(cachePath))
        {
            try
            {
                Directory.Delete(cachePath, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear browser cache");
            }
        }
    }

    // Event handlers
    private void OnAddressChanged(Guid tabId, string url)
    {
        if (_tabs.TryGetValue(tabId, out var tab))
        {
            tab.Model = tab.Model with { Url = url };
            AddressChanged?.Invoke(this, (tabId, url));
            
            // Check for OAuth callback
            if (_oauthTcs != null && !_oauthTcs.Task.IsCompleted && 
                _oauthTabId == tabId && url.Contains("code="))
            {
                var callback = ParseOAuthCallback(url);
                _oauthTcs.TrySetResult(callback);
            }
        }
    }

    private void OnTitleChanged(Guid tabId, string title)
    {
        if (_tabs.TryGetValue(tabId, out var tab))
        {
            tab.Model = tab.Model with { Title = title };
            TitleChanged?.Invoke(this, (tabId, title));
            
            // Add to history
            if (!string.IsNullOrEmpty(title) && !tab.Model.IsLoading)
            {
                _ = AddHistoryItemAsync(title, tab.Model.Url);
            }
        }
    }

    private void OnLoadingProgress(Guid tabId, double progress)
    {
        if (_tabs.TryGetValue(tabId, out var tab))
        {
            tab.Model = tab.Model with { LoadingProgress = progress };
            LoadingProgressChanged?.Invoke(this, (tabId, progress));
        }
    }

    private void OnLoadingStateChanged(Guid tabId, bool isLoading)
    {
        if (_tabs.TryGetValue(tabId, out var tab))
        {
            tab.Model = tab.Model with 
            { 
                IsLoading = isLoading,
                State = isLoading ? BrowserTabState.Loading : BrowserTabState.Loaded,
                CanGoBack = tab.Browser.CanGoBack,
                CanGoForward = tab.Browser.CanGoForward
            };
            LoadingStateChanged?.Invoke(this, (tabId, isLoading));
        }
    }

    private void OnDownloadStarted(BrowserDownload download)
    {
        _downloads[download.Id] = download;
        DownloadStarted?.Invoke(this, download);
    }

    private void OnDownloadProgress(BrowserDownload download)
    {
        _downloads[download.Id] = download;
        DownloadProgressChanged?.Invoke(this, download);
    }

    private void OnDownloadCompleted(BrowserDownload download)
    {
        _downloads[download.Id] = download;
        DownloadCompleted?.Invoke(this, download);
    }

    private void OnContextMenuRequested(Guid tabId, List<BrowserContextMenuItem> menuItems)
    {
        ContextMenuRequested?.Invoke(this, (tabId, menuItems));
    }

    private bool OnPopup(string targetUrl, string targetFrameName)
    {
        if (_settings.BlockPopups)
        {
            _logger.LogDebug("Blocked popup to {Url}", targetUrl);
            return false; // Block popup
        }
        
        // Create new tab for popup
        _ = CreateTabAsync(targetUrl, activate: false);
        return false; // Cancel default popup behavior
    }

    private void OnBeforePopupClose(Guid tabId)
    {
        // Handle popup close if needed
    }

    private bool OnKeyEvent(Guid tabId, KeyType type, int code, int modifiers)
    {
        // Handle keyboard shortcuts
        // Return false to allow default processing
        return false;
    }

    private static OAuthCallback ParseOAuthCallback(string url)
    {
        var uri = new Uri(url);
        var query = ParseQueryString(uri.Query);
        
        var callback = new OAuthCallback
        {
            Code = query.GetValueOrDefault("code") ?? string.Empty,
            State = query.GetValueOrDefault("state"),
            Error = query.GetValueOrDefault("error")
        };

        // Add any additional query parameters
        foreach (var kvp in query)
        {
            if (kvp.Key != "code" && kvp.Key != "state" && kvp.Key != "error")
            {
                callback.AdditionalData[kvp.Key] = kvp.Value;
            }
        }

        return callback;
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query)) return result;

        // Remove leading ? if present
        if (query.StartsWith('?'))
            query = query.Substring(1);

        var pairs = query.Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = Uri.UnescapeDataString(parts[0]);
                var value = Uri.UnescapeDataString(parts[1]);
                result[key] = value;
            }
            else if (parts.Length == 1)
            {
                result[Uri.UnescapeDataString(parts[0])] = string.Empty;
            }
        }

        return result;
    }

    private class BrowserTabInstance
    {
        public required BrowserTab Model { get; set; }
        public required ChromiumWebBrowser Browser { get; set; }
        public required CustomDownloadHandler DownloadHandler { get; set; }
        public bool IsActive { get; set; }
    }
}
