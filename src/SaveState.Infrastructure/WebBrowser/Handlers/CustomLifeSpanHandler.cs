using CefSharp;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.WebBrowser.Handlers;

/// <summary>
/// Handles browser lifespan events including popup windows and tab creation.
/// </summary>
public sealed class CustomLifeSpanHandler : ILifeSpanHandler
{
    private readonly ILogger _logger;
    private readonly Func<string, string, bool> _onPopup;
    private readonly Action<Guid> _onBeforePopupClose;
    private readonly Dictionary<int, Guid> _popupTabIds = new();

    public CustomLifeSpanHandler(
        ILogger logger,
        Func<string, string, bool> onPopup,
        Action<Guid> onBeforePopupClose)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _onPopup = onPopup ?? throw new ArgumentNullException(nameof(onPopup));
        _onBeforePopupClose = onBeforePopupClose ?? throw new ArgumentNullException(nameof(onBeforePopupClose));
    }

    public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        _logger.LogDebug("DoClose called for browser");
        
        if (_popupTabIds.TryGetValue(browser.Identifier, out var tabId))
        {
            _onBeforePopupClose(tabId);
            _popupTabIds.Remove(browser.Identifier);
        }
        
        return false; // Allow the close
    }

    public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        _logger.LogDebug("Browser created with ID {BrowserId}", browser.Identifier);
    }

    public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        _logger.LogDebug("Browser closing with ID {BrowserId}", browser.Identifier);
        
        if (_popupTabIds.TryGetValue(browser.Identifier, out var tabId))
        {
            _popupTabIds.Remove(browser.Identifier);
        }
    }

    public bool OnBeforePopup(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        string targetUrl,
        string targetFrameName,
        WindowOpenDisposition targetDisposition,
        bool userGesture,
        IPopupFeatures popupFeatures,
        IWindowInfo windowInfo,
        IBrowserSettings browserSettings,
        ref bool noJavascriptAccess,
        out IWebBrowser newBrowser)
    {
        _logger.LogDebug("Popup requested: {Url}, Target: {Target}", targetUrl, targetFrameName);

        // Let the service handle popup creation
        var shouldCancel = _onPopup(targetUrl, targetFrameName);
        
        newBrowser = null!;
        return shouldCancel; // Return true to cancel default popup, false to allow
    }
}
