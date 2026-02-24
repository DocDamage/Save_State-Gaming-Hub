using CefSharp;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.WebBrowser.Models;

namespace SaveState.Infrastructure.WebBrowser.Handlers;

/// <summary>
/// Handles file downloads from the browser.
/// </summary>
public sealed class CustomDownloadHandler : IDownloadHandler
{
    private readonly ILogger _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Action<BrowserDownload> _onDownloadStarted;
    private readonly Action<BrowserDownload> _onDownloadProgress;
    private readonly Action<BrowserDownload> _onDownloadCompleted;
    private readonly Dictionary<int, BrowserDownload> _activeDownloads = new();

    public CustomDownloadHandler(
        ILogger logger,
        ITimeProvider timeProvider,
        Action<BrowserDownload> onDownloadStarted,
        Action<BrowserDownload> onDownloadProgress,
        Action<BrowserDownload> onDownloadCompleted)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _onDownloadStarted = onDownloadStarted ?? throw new ArgumentNullException(nameof(onDownloadStarted));
        _onDownloadProgress = onDownloadProgress ?? throw new ArgumentNullException(nameof(onDownloadProgress));
        _onDownloadCompleted = onDownloadCompleted ?? throw new ArgumentNullException(nameof(onDownloadCompleted));
    }

    public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
    {
        // Allow all downloads
        return true;
    }

    public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
    {
        _logger.LogInformation("Download starting: {FileName} from {Url}", 
            downloadItem.SuggestedFileName, 
            downloadItem.Url);

        var download = new BrowserDownload
        {
            Id = Guid.NewGuid(),
            FileName = downloadItem.SuggestedFileName,
            Url = downloadItem.Url,
            MimeType = downloadItem.MimeType,
            TotalBytes = downloadItem.TotalBytes,
            State = DownloadState.InProgress,
            StartedAt = _timeProvider.Now
        };

        lock (_activeDownloads)
        {
            _activeDownloads[downloadItem.Id] = download;
        }
        _onDownloadStarted(download);

        if (!callback.IsDisposed)
        {
            using (callback)
            {
                var downloadPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    downloadItem.SuggestedFileName);

                download.SavePath = downloadPath;
                callback.Continue(downloadPath, showDialog: false);
            }
        }

        return true;
    }

    public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
    {
        BrowserDownload? download;
        lock (_activeDownloads)
        {
            if (!_activeDownloads.TryGetValue(downloadItem.Id, out download))
                return;
        }

        download.ReceivedBytes = downloadItem.ReceivedBytes;
        download.TotalBytes = downloadItem.TotalBytes;

        if (downloadItem.IsComplete)
        {
            download.State = downloadItem.IsCancelled ? DownloadState.Canceled : 
                            downloadItem.FullPath != null ? DownloadState.Completed : DownloadState.Failed;
            download.CompletedAt = _timeProvider.Now;
            
            if (download.State == DownloadState.Completed)
            {
                download.SavePath = downloadItem.FullPath;
                _logger.LogInformation("Download completed: {FileName}", download.FileName);
            }
            else if (download.State == DownloadState.Canceled)
            {
                _logger.LogInformation("Download canceled: {FileName}", download.FileName);
            }
            else
            {
                _logger.LogWarning("Download failed: {FileName}", download.FileName);
            }

            _onDownloadCompleted(download);
            
            lock (_activeDownloads)
            {
                _activeDownloads.Remove(downloadItem.Id);
            }
        }
        else if (downloadItem.IsInProgress)
        {
            _onDownloadProgress(download);
        }
    }

    /// <summary>
    /// Cancels an active download by its CefSharp download ID.
    /// </summary>
    public bool CancelDownload(int downloadId)
    {
        // Note: CefSharp doesn't provide direct cancellation through callback storage
        // The callback would need to be stored in OnBeforeDownload
        _logger.LogDebug("Cancel download requested for ID {DownloadId}", downloadId);
        return false;
    }

    /// <summary>
    /// Gets an active download by ID.
    /// </summary>
    public BrowserDownload? GetDownload(int downloadId)
    {
        lock (_activeDownloads)
        {
            return _activeDownloads.TryGetValue(downloadId, out var download) ? download : null;
        }
    }
}
