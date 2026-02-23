using CefSharp;
using CefSharp.Enums;
using CefSharp.Event;
using CefSharp.Structs;
using Microsoft.Extensions.Logging;


namespace SaveState.Infrastructure.WebBrowser.Handlers;

/// <summary>
/// Handles display-related events such as address changes, title changes, and loading state.
/// </summary>
public sealed class CustomDisplayHandler : IDisplayHandler
{
    private readonly ILogger _logger;
    private readonly Action<Guid, string> _onAddressChanged;
    private readonly Action<Guid, string> _onTitleChanged;
    private readonly Action<Guid, double> _onLoadingProgress;
    private readonly Action<Guid, bool> _onLoadingStateChanged;
    private Guid _tabId;

    public CustomDisplayHandler(
        ILogger logger,
        Action<Guid, string> onAddressChanged,
        Action<Guid, string> onTitleChanged,
        Action<Guid, double> onLoadingProgress,
        Action<Guid, bool> onLoadingStateChanged)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _onAddressChanged = onAddressChanged ?? throw new ArgumentNullException(nameof(onAddressChanged));
        _onTitleChanged = onTitleChanged ?? throw new ArgumentNullException(nameof(onTitleChanged));
        _onLoadingProgress = onLoadingProgress ?? throw new ArgumentNullException(nameof(onLoadingProgress));
        _onLoadingStateChanged = onLoadingStateChanged ?? throw new ArgumentNullException(nameof(onLoadingStateChanged));
    }

    public void SetTabId(Guid tabId)
    {
        _tabId = tabId;
    }

    public void OnAddressChanged(IWebBrowser chromiumWebBrowser, AddressChangedEventArgs addressChangedArgs)
    {
        _logger.LogTrace("Address changed to {Url}", addressChangedArgs.Address);
        _onAddressChanged(_tabId, addressChangedArgs.Address);
    }

    public void OnTitleChanged(IWebBrowser chromiumWebBrowser, TitleChangedEventArgs titleChangedArgs)
    {
        _logger.LogTrace("Title changed to {Title}", titleChangedArgs.Title);
        _onTitleChanged(_tabId, titleChangedArgs.Title);
    }

    public void OnLoadingProgressChange(IWebBrowser chromiumWebBrowser, IBrowser browser, double progress)
    {
        _onLoadingProgress(_tabId, progress);
    }

    public void OnLoadingStateChange(IWebBrowser chromiumWebBrowser, LoadingStateChangedEventArgs loadingStateChangedArgs)
    {
        var isLoading = loadingStateChangedArgs.IsLoading;
        _logger.LogTrace("Loading state changed: {IsLoading}", isLoading);
        _onLoadingStateChanged(_tabId, isLoading);
    }

    public bool OnConsoleMessage(IWebBrowser chromiumWebBrowser, ConsoleMessageEventArgs consoleMessageArgs)
    {
        var level = consoleMessageArgs.Level;
        var message = consoleMessageArgs.Message;
        var source = consoleMessageArgs.Source;

        switch (level)
        {
            case LogSeverity.Error:
                _logger.LogWarning("Browser console error [{Source}]: {Message}", source, message);
                break;
            case LogSeverity.Warning:
                _logger.LogDebug("Browser console warning [{Source}]: {Message}", source, message);
                break;
            default:
                _logger.LogTrace("Browser console [{Source}]: {Message}", source, message);
                break;
        }

        return false; // Don't suppress the message
    }

    public void OnStatusMessage(IWebBrowser chromiumWebBrowser, StatusMessageEventArgs statusMessageArgs)
    {
        _logger.LogTrace("Status message: {Message}", statusMessageArgs.Value);
    }

    public bool OnTooltipChanged(IWebBrowser chromiumWebBrowser, ref string text)
    {
        // Tooltip handling if needed
        return false;
    }

    public bool OnAutoResize(IWebBrowser chromiumWebBrowser, IBrowser browser, CefSharp.Structs.Size newSize)
    {
        // Auto resize handling
        return false;
    }

    public bool OnCursorChange(IWebBrowser chromiumWebBrowser, IBrowser browser, nint cursor, CursorType type, CursorInfo customCursorInfo)
    {
        // Cursor change handling
        return false;
    }

    public void OnFaviconUrlChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IList<string> iconUrls)
    {
        // Favicon URL change handling
    }

    public void OnFullscreenModeChange(IWebBrowser chromiumWebBrowser, IBrowser browser, bool fullscreen)
    {
        _logger.LogTrace("Fullscreen mode changed: {Fullscreen}", fullscreen);
    }
}
