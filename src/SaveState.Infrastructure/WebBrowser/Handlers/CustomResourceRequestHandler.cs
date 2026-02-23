using CefSharp;
using CefSharp.Handler;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.WebBrowser.Handlers;

/// <summary>
/// Handles resource requests for filtering ads, modifying headers, and controlling resource loading.
/// </summary>
public sealed class CustomResourceRequestHandler : ResourceRequestHandler
{
    private readonly ILogger _logger;
    private readonly List<string> _blockedDomains;
    private readonly Dictionary<string, string> _customHeaders;

    // Common ad/tracking domain patterns
    private static readonly HashSet<string> AdDomainPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "googleadservices", "googlesyndication", "doubleclick", "google-analytics",
        "facebook.com/tr", "facebook.net", "fbcdn.net",
        "analytics", "tracking", "telemetry", "metrics",
        "adsystem", "advertising", "adnxs", "adsrvr", "adsymptotic",
        "amazon-adsystem", "advertising.amazon",
        "outbrain", "taboola", "mgid",
        "scorecardresearch", "quantserve", "quantcount"
    };

    public CustomResourceRequestHandler(
        ILogger logger,
        List<string> blockedDomains,
        Dictionary<string, string> customHeaders)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _blockedDomains = blockedDomains ?? new List<string>();
        _customHeaders = customHeaders ?? new Dictionary<string, string>();
    }

    protected override CefReturnValue OnBeforeResourceLoad(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IRequest request,
        IRequestCallback callback)
    {
        var url = request.Url;

        // Block ads and tracking
        if (ShouldBlockResource(url))
        {
            _logger.LogTrace("Blocked resource: {Url}", url);
            return CefReturnValue.Cancel;
        }

        // Add custom headers
        if (_customHeaders.Any())
        {
            foreach (var header in _customHeaders)
            {
                request.SetHeaderByName(header.Key, header.Value, overwrite: true);
            }
        }

        // Set Do Not Track header
        request.SetHeaderByName("DNT", "1", overwrite: true);

        return CefReturnValue.Continue;
    }

    protected override void OnResourceLoadComplete(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IRequest request,
        IResponse response,
        UrlRequestStatus status,
        long receivedContentLength)
    {
        if (status != UrlRequestStatus.Success)
        {
            _logger.LogTrace("Resource load completed with status {Status}: {Url}", status, request.Url);
        }
    }

    protected override bool OnProtocolExecution(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request)
    {
        // Allow protocol execution for common schemes
        var scheme = new Uri(request.Url).Scheme.ToLowerInvariant();
        return scheme is "http" or "https" or "file" or "data";
    }

    protected override void OnResourceRedirect(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IRequest request,
        IResponse response,
        ref string newUrl)
    {
        _logger.LogTrace("Resource redirect from {OldUrl} to {NewUrl}", request.Url, newUrl);
    }

    protected override bool OnResourceResponse(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        IRequest request,
        IResponse response)
    {
        // Return false to allow the response to proceed
        return false;
    }

    private bool ShouldBlockResource(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();

            // Check custom blocked domains
            if (_blockedDomains.Any(blocked => 
                host.Contains(blocked.ToLowerInvariant()) || 
                uri.AbsoluteUri.Contains(blocked, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Check ad patterns
            if (AdDomainPatterns.Any(pattern => host.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Block common ad file types
            var path = uri.AbsolutePath.ToLowerInvariant();
            if (path.EndsWith(".ads.js") || 
                path.Contains("/ads/") ||
                path.Contains("/tracking/") ||
                path.Contains("/analytics/"))
            {
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
