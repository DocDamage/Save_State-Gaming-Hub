using CefSharp;
using CefSharp.Handler;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.WebBrowser.Handlers;

/// <summary>
/// Custom request handler for managing web requests, blocking domains, and adding custom headers.
/// </summary>
public sealed class CustomRequestHandler : RequestHandler
{
    private readonly ILogger _logger;
    private readonly List<string> _blockedDomains;
    private readonly Dictionary<string, string> _customHeaders;

    public CustomRequestHandler(
        ILogger logger, 
        List<string> blockedDomains,
        Dictionary<string, string> customHeaders)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blockedDomains = blockedDomains ?? new List<string>();
        _customHeaders = customHeaders ?? new Dictionary<string, string>();
    }

    protected override IResourceRequestHandler GetResourceRequestHandler(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IRequest request,
        bool isNavigation,
        bool isDownload,
        string requestInitiator,
        ref bool disableDefaultHandling)
    {
        return new CustomResourceRequestHandler(_logger, _blockedDomains, _customHeaders);
    }

    protected override bool OnBeforeBrowse(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IRequest request,
        bool userGesture,
        bool isRedirect)
    {
        var url = request.Url;
        
        // Check if domain is blocked
        if (IsDomainBlocked(url))
        {
            _logger.LogDebug("Blocked navigation to {Url}", url);
            return true; // Cancel navigation
        }

        return false; // Allow navigation
    }

    protected override bool OnOpenUrlFromTab(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        string targetUrl,
        WindowOpenDisposition targetDisposition,
        bool userGesture)
    {
        _logger.LogDebug("Opening URL from tab: {Url}", targetUrl);
        return false; // Allow default handling
    }

    protected override void OnDocumentAvailableInMainFrame(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        _logger.LogDebug("Document available in main frame");
    }

    private bool IsDomainBlocked(string url)
    {
        if (string.IsNullOrEmpty(url) || !_blockedDomains.Any())
            return false;

        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();

            return _blockedDomains.Any(blocked => 
                host.Contains(blocked.ToLowerInvariant()) || 
                blocked.ToLowerInvariant().Contains(host));
        }
        catch
        {
            return false;
        }
    }
}
