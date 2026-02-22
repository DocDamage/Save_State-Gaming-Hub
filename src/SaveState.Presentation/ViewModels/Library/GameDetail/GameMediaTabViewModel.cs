using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.UserManagement.Services;
using SaveState.Core.Ai.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Services;
using SaveState.Core.Sync;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

/// <summary>
/// View model for the Game Media tab.
/// </summary>
public partial class GameMediaTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IUserContextService _userContextService;
    private readonly IDialogService _dialogService;
    private readonly IGameMediaService _gameMediaService;
    private readonly INotificationService _notificationService;
    private readonly IClipboardService _clipboardService;
    private readonly IImageAnalysisService? _imageAnalysisService;
    private readonly ILogger<GameMediaTabViewModel> _logger;
    private GameId? _currentGameId;

    [ObservableProperty]
    private string _mediaCountText = "0 media files";

    [ObservableProperty]
    private int _screenshotCount;

    [ObservableProperty]
    private int _videoCount;

    [ObservableProperty]
    private string _totalSize = "0 MB";

    [ObservableProperty]
    private string _lastCaptureText = "Never";

    [ObservableProperty]
    private ObservableCollection<GameMediaItemViewModel> _mediaItems = new();

    [ObservableProperty]
    private ObservableCollection<GameRecentMediaViewModel> _recentCaptures = new();

    [ObservableProperty]
    private ObservableCollection<string> _viewModeOptions = new() { "Grid", "List", "Timeline" };

    [ObservableProperty]
    private string _selectedViewMode = "Grid";

    [ObservableProperty]
    private bool _showScreenshots = true;

    [ObservableProperty]
    private bool _showVideos = true;

    [ObservableProperty]
    private bool _showFavorites;

    [ObservableProperty]
    private ObservableCollection<string> _dateRangeOptions = new() { "All", "Today", "This Week", "This Month", "This Year" };

    [ObservableProperty]
    private string _selectedDateRange = "All";

    [ObservableProperty]
    private double _storageUsagePercentage;

    [ObservableProperty]
    private string _storageUsedText = "0 MB";

    [ObservableProperty]
    private string _storageAvailableText = "0 GB available";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _scanStatusText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _detectedTags = new();

    private readonly ISyncService _syncService;
    private readonly ITimeProvider _timeProvider;

    public GameMediaTabViewModel(
        IMediator mediator,
        IUserContextService userContextService,
        IDialogService dialogService,
        IGameMediaService gameMediaService,
        INotificationService notificationService,
        ISyncService syncService,
        IClipboardService clipboardService,
        IImageAnalysisService? imageAnalysisService,
        ILogger<GameMediaTabViewModel> logger,
        ITimeProvider timeProvider)
    {
        _mediator = mediator;
        _userContextService = userContextService;
        _dialogService = dialogService;
        _gameMediaService = gameMediaService;
        _notificationService = notificationService;
        _syncService = syncService;
        _clipboardService = clipboardService;
        _imageAnalysisService = imageAnalysisService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task LoadDataAsync(GameId gameId)
    {
        try
        {
            _currentGameId = gameId;

            var userId = _userContextService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("No current user context - cannot load media");
                MediaCountText = "0 media files";
                return;
            }

            var query = new GetGameMediaQuery(gameId.Value, userId.Value);
            var mediaItems = await _mediator.Send(query).ConfigureAwait(false);

            ScreenshotCount = mediaItems.Count(m => m.MediaType == MediaType.Screenshot);
            VideoCount = mediaItems.Count(m => m.MediaType == MediaType.Video);

            var totalCount = mediaItems.Count;
            MediaCountText = $"{totalCount} media file{(totalCount == 1 ? "" : "s")}";

            // Calculate total size
            var totalBytes = mediaItems.Sum(m => m.FileSizeBytes);
            TotalSize = FormatFileSize(totalBytes);
            StorageUsedText = TotalSize;

            // Find most recent capture
            var mostRecent = mediaItems.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            LastCaptureText = mostRecent != null ? FormatDateTime(mostRecent.CreatedAt) : "Never";

            // Calculate storage usage percentage
            try
            {
                var driveInfo = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)) ?? "C:\\");
                var usedBytes = totalBytes;
                var totalDriveBytes = driveInfo.TotalSize;
                StorageUsagePercentage = totalDriveBytes > 0 ? (double)usedBytes / totalDriveBytes * 100 : 0;
                StorageAvailableText = $"{FormatFileSize(driveInfo.AvailableFreeSpace)} available";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate storage usage");
                StorageUsagePercentage = 0;
                StorageAvailableText = "Unknown";
            }
            StorageUsagePercentage = 0.0;
            StorageAvailableText = "0 GB available";

            // Populate media items
            MediaItems.Clear();
            foreach (var media in mediaItems.OrderByDescending(m => m.CreatedAt))
            {
                MediaItems.Add(new GameMediaItemViewModel
                {
                    MediaId = media.Id,
                    FileName = System.IO.Path.GetFileName(media.FilePath),
                    DateText = FormatDateTime(media.CreatedAt),
                    SizeText = FormatFileSize(media.FileSizeBytes),
                    PreviewUrl = media.ThumbnailPath ?? media.FilePath,
                    FilePath = media.FilePath,
                    IsVideo = media.MediaType == MediaType.Video,
                    IsFavorite = media.IsFavorite,
                    Opacity = "1.0",
                    DeleteAction = OnDeleteMediaItemAsync,
                    CopyAction = OnCopyMediaItemAsync
                });
            }

            // Populate recent captures (last 5)
            RecentCaptures.Clear();
            foreach (var media in mediaItems.OrderByDescending(m => m.CreatedAt).Take(5))
            {
                RecentCaptures.Add(new GameRecentMediaViewModel
                {
                    FileName = System.IO.Path.GetFileName(media.FilePath),
                    DateText = FormatDateTime(media.CreatedAt),
                    SizeText = FormatFileSize(media.FileSizeBytes),
                    ThumbnailUrl = media.ThumbnailPath ?? media.FilePath
                });
            }

            _logger.LogInformation("Loaded {Count} media items for game {GameId}", mediaItems.Count, gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load media for game {GameId}", gameId);
        }
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";

        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }

    private string FormatDateTime(DateTime dateTime)
    {
        var now = _timeProvider.UtcNow;
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1)
            return "Just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";

        return dateTime.ToString("MMM d, yyyy");
    }

    [RelayCommand]
    private async Task TakeScreenshot()
    {
        if (_currentGameId == null) return;

        try
        {
            var command = new SaveState.Application.GameLibrary.Commands.CaptureScreenshotCommand(_currentGameId.Value);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess("Screenshot captured!", "Media");
                await LoadDataAsync(_currentGameId);
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", $"Failed to capture: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to take screenshot");
            await _dialogService.ShowErrorAsync("Error", "Screenshot capture failed.");
        }
    }

    [RelayCommand]
    private async Task RecordVideo()
    {
        if (_currentGameId == null) return;

        try
        {
            // For a complete implementation, we'd maybe ask for duration or just start/stop
            // Here we trigger a conceptual 30s recording command
             var command = new SaveState.Application.GameLibrary.Commands.RecordVideoCommand(_currentGameId.Value, TimeSpan.FromSeconds(30));
             var result = await _mediator.Send(command);

             if (result.IsSuccess)
             {
                 _notificationService.ShowSuccess("Video recorded (30s)!", "Media");
                 await LoadDataAsync(_currentGameId);
             }
             else
             {
                 await _dialogService.ShowErrorAsync("Error", $"Failed to record: {result.Error}");
             }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record video");
            await _dialogService.ShowErrorAsync("Error", "Video recording failed.");
        }
    }

    [RelayCommand]
    private async Task OpenFolder()
    {
        try
        {
            // Open the media folder in file explorer
            var mediaPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SaveStateReborn",
                "Media");

            if (System.IO.Directory.Exists(mediaPath))
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = mediaPath,
                    UseShellExecute = true,
                    Verb = "open"
                };
                System.Diagnostics.Process.Start(psi);
            }
            else
            {
                await _dialogService.ShowInformationAsync("Folder Not Found", "The media folder does not exist yet. Capture some screenshots or videos first!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open media folder");
            await _dialogService.ShowErrorAsync("Error", "Failed to open the media folder.");
        }
    }

    [RelayCommand]
    private async Task UploadSelected()
    {
         var selectedCount = MediaItems.Count(x => x.IsSelected);
         if (selectedCount == 0) {
             _notificationService.ShowWarning("No items selected", "Upload");
             return;
         }

         if (_syncService.Status == SyncStatus.NotConfigured)
         {
             await _dialogService.ShowErrorAsync("Cloud Not Configured", "Please configure a cloud provider in settings first.");
             return;
         }

         try
         {
             _notificationService.ShowInfo($"Starting sync for {selectedCount} items...", "Cloud Upload");

             // Trigger sync push to upload changes
             var result = await _syncService.PushAsync();

             if (result.Success)
             {
                 _notificationService.ShowSuccess($"Uploaded pending changes to {_syncService.ActiveProviderName}", "Upload Complete");
             }
             else
             {
                 _notificationService.ShowError("Upload failed: " + string.Join(", ", result.Errors), "Upload Error");
             }
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Failed to upload media");
             _notificationService.ShowError("Failed to upload media items", "Error");
         }
    }

    [RelayCommand]
    private async Task ExportSelected()
    {
         var selectedItems = MediaItems.Where(x => x.IsSelected).ToList();
         if (!selectedItems.Any()) {
             await _dialogService.ShowInformationAsync("Export", "No items selected.");
             return;
         }

         var folder = await _dialogService.ShowFolderPickerAsync("Select Export Folder");
         if (string.IsNullOrEmpty(folder)) return;

         try
         {
             int count = 0;
             foreach (var item in selectedItems)
             {
                 if (File.Exists(item.FilePath))
                 {
                     var dest = Path.Combine(folder, item.FileName);
                     File.Copy(item.FilePath, dest, true);
                     count++;
                 }
             }
             _notificationService.ShowSuccess($"Exported {count} files successfully.", "Export Complete");
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "Failed to export media");
             _notificationService.ShowError("Failed to export some files.");
         }
    }

    private async Task OnDeleteMediaItemAsync(GameMediaItemViewModel item)
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Media",
            $"Are you sure you want to delete '{item.FileName}'?");

        if (confirmed)
        {
            try
            {
                var result = await _mediator.Send(new SaveState.Application.GameLibrary.Commands.DeleteGameMediaCommand(item.MediaId));
                if (result.IsSuccess)
                {
                    MediaItems.Remove(item);
                    _notificationService.ShowSuccess($"Deleted {item.FileName}", "Media Deleted");
                }
                else
                {
                    _notificationService.ShowError("Failed to delete media item");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete media {MediaId}", item.MediaId);
                _notificationService.ShowError("An error occurred while deleting the media");
            }
        }
    }

    private async Task OnCopyMediaItemAsync(GameMediaItemViewModel item)
    {
        if (!string.IsNullOrEmpty(item.FilePath))
        {
            if (item.IsImage)
            {
                // Copy actual image content
                await _clipboardService.SetImageAsync(item.FilePath);
                _notificationService.ShowSuccess($"Copied image to clipboard", "Copied");
            }
            else
            {
                // Copy file path for videos or other types
                await _clipboardService.SetTextAsync(item.FilePath);
                _notificationService.ShowSuccess($"Copied path to clipboard", "Copied");
            }
        }
    }

    /// <summary>
    /// Scans selected screenshots using AI Vision to detect content and suggest tags.
    /// PHASE 1: Core Services - Screenshot Scanning Feature.
    /// </summary>
    [RelayCommand]
    private async Task ScanSelectedForTags()
    {
        if (_imageAnalysisService == null)
        {
            _notificationService.ShowWarning("Image analysis service not available", "Feature Unavailable");
            return;
        }

        if (_currentGameId == null)
        {
            _notificationService.ShowWarning("No game selected", "Scan");
            return;
        }

        var selectedItems = MediaItems.Where(x => x.IsSelected && x.IsImage).ToList();
        if (!selectedItems.Any())
        {
            // If nothing selected, scan all screenshots
            selectedItems = MediaItems.Where(x => x.IsImage).Take(5).ToList();
            if (!selectedItems.Any())
            {
                _notificationService.ShowInfo("No screenshots to analyze", "Scan");
                return;
            }
        }

        try
        {
            IsScanning = true;
            ScanStatusText = $"Analyzing {selectedItems.Count} screenshot(s)...";
            DetectedTags.Clear();

            var allTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in selectedItems)
            {
                if (string.IsNullOrEmpty(item.FilePath) || !File.Exists(item.FilePath))
                    continue;

                ScanStatusText = $"Analyzing {item.FileName}...";

                var result = await _imageAnalysisService.GetSuggestedTagsAsync(item.FilePath, 10);

                if (result.IsSuccess)
                {
                    foreach (var tag in result.Value)
                    {
                        allTags.Add(tag);
                    }
                    _logger.LogDebug("Detected {TagCount} tags from {FileName}", result.Value.Count, item.FileName);
                }
                else
                {
                    _logger.LogWarning("Failed to analyze {FileName}: {Error}", item.FileName, result.Error);
                }
            }

            // Populate detected tags
            foreach (var tag in allTags.Take(15))
            {
                DetectedTags.Add(tag);
            }

            ScanStatusText = $"Found {DetectedTags.Count} unique tags";

            if (DetectedTags.Any())
            {
                // Ask user if they want to add these tags to the game
                var tagList = string.Join(", ", DetectedTags.Take(10));
                var confirmMessage = DetectedTags.Count > 10
                    ? $"Detected tags: {tagList}... and {DetectedTags.Count - 10} more.\n\nWould you like to add these tags to the game?"
                    : $"Detected tags: {tagList}\n\nWould you like to add these tags to the game?";

                var confirmed = await _dialogService.ShowConfirmationAsync(
                    "Screenshot Analysis Complete",
                    confirmMessage);

                if (confirmed)
                {
                    await AddDetectedTagsToGame();
                }
            }
            else
            {
                _notificationService.ShowInfo("No relevant tags detected in the screenshots", "Scan Complete");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan screenshots for tags");
            _notificationService.ShowError("Failed to analyze screenshots", "Scan Error");
            ScanStatusText = "Scan failed";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task AddDetectedTagsToGame()
    {
        if (_currentGameId == null || !DetectedTags.Any())
            return;

        try
        {
            // Get current tags
            var gameQuery = new GetGameByIdQuery(_currentGameId);
            var game = await _mediator.Send(gameQuery);

            if (game == null)
            {
                _notificationService.ShowError("Could not find game", "Error");
                return;
            }

            // Merge existing tags with new detected tags
            var existingTags = game.Tags.ToList();
            var newTags = DetectedTags
                .Where(t => !existingTags.Contains(t, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (!newTags.Any())
            {
                _notificationService.ShowInfo("All detected tags already exist on the game", "Tags");
                return;
            }

            var allTags = existingTags.Concat(newTags).ToList();

            var command = new UpdateGameTagsCommand(_currentGameId.Value, allTags);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Added {newTags.Count} new tag(s) to the game", "Tags Updated");
                _logger.LogInformation("Added {Count} tags to game {GameId} from screenshot analysis",
                    newTags.Count, _currentGameId);
            }
            else
            {
                _notificationService.ShowError($"Failed to update tags: {result.Error}", "Error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add detected tags to game");
            _notificationService.ShowError("Failed to update game tags", "Error");
        }
    }
}

/// <summary>
/// View model for individual media items.
/// </summary>
public partial class GameMediaItemViewModel : ObservableObject
{
    public Func<GameMediaItemViewModel, Task>? DeleteAction { get; set; }
    public Func<GameMediaItemViewModel, Task>? CopyAction { get; set; }

    [ObservableProperty]
    private Guid _mediaId;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _dateText = string.Empty;

    [ObservableProperty]
    private string _sizeText = string.Empty;

    [ObservableProperty]
    private string? _previewUrl;

    [ObservableProperty]
    private bool _isVideo;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private string _opacity = "1.0";

    public string VideoIndicator => IsVideo ? "Visible" : "Collapsed";

    public string FilePath { get; set; }
    public bool IsImage => !IsVideo;

    [RelayCommand]
    private async Task View()
    {
        try
        {
            if (!string.IsNullOrEmpty(FilePath) && System.IO.File.Exists(FilePath))
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = FilePath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
        }
        catch (Exception)
        {
            // Ignore for now
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Copy()
    {
        if (CopyAction != null) await CopyAction.Invoke(this);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (DeleteAction != null) await DeleteAction.Invoke(this);
    }
}

/// <summary>
/// View model for recent media captures.
/// </summary>
public class GameRecentMediaViewModel : ObservableObject
{
    public string FileName { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string SizeText { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}
