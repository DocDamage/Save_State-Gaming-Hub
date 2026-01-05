using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.UserManagement.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

/// <summary>
/// View model for the Game Media tab.
/// </summary>
public partial class GameMediaTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IUserContextService _userContextService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<GameMediaTabViewModel> _logger;

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

    public GameMediaTabViewModel(
        IMediator mediator,
        IUserContextService userContextService,
        IDialogService dialogService,
        ILogger<GameMediaTabViewModel> logger)
    {
        _mediator = mediator;
        _userContextService = userContextService;
        _dialogService = dialogService;
        _logger = logger;
    }

    public async Task LoadDataAsync(GameId gameId)
    {
        try
        {
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

            // TODO: Calculate actual storage usage percentage against available disk space
            StorageUsagePercentage = 0.0;
            StorageAvailableText = "0 GB available";

            // Populate media items
            MediaItems.Clear();
            foreach (var media in mediaItems.OrderByDescending(m => m.CreatedAt))
            {
                MediaItems.Add(new GameMediaItemViewModel
                {
                    FileName = System.IO.Path.GetFileName(media.FilePath),
                    DateText = FormatDateTime(media.CreatedAt),
                    SizeText = FormatFileSize(media.FileSizeBytes),
                    PreviewUrl = media.ThumbnailPath ?? media.FilePath,
                    IsVideo = media.MediaType == MediaType.Video,
                    IsFavorite = media.IsFavorite,
                    Opacity = "1.0"
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

    private static string FormatDateTime(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
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
        await _dialogService.ShowInformationAsync("Screenshot", "Screenshot capture via overlay coming soon.");
    }

    [RelayCommand]
    private async Task RecordVideo()
    {
        await _dialogService.ShowInformationAsync("Record Video", "Video recording coming soon.");
    }

    [RelayCommand]
    private async Task OpenMediaFolder()
    {
        await _dialogService.ShowInformationAsync("Info", "Opening file explorer...");
        // TODO: Implement actual folder opening
    }

    [RelayCommand]
    private async Task ExportSelected()
    {
         var selectedCount = MediaItems.Count(x => x.IsSelected);
         if (selectedCount == 0) {
             await _dialogService.ShowInformationAsync("Export", "No items selected.");
             return;
         }
         await _dialogService.ShowInformationAsync("Export", $"Exporting {selectedCount} items coming soon.");
    }

    [RelayCommand]
    private async Task FavoriteSelected()
    {
         var selectedCount = MediaItems.Count(x => x.IsSelected);
         if (selectedCount == 0) return;

         foreach(var item in MediaItems.Where(x => x.IsSelected))
         {
             item.IsFavorite = !item.IsFavorite;
             // TODO: Persist to backend
         }
         await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {
         var selectedCount = MediaItems.Count(x => x.IsSelected);
         if (selectedCount == 0) return;

         if (await _dialogService.ShowConfirmationAsync("Delete Media", $"Are you sure you want to delete {selectedCount} items?"))
         {
             // TODO: Delete items from backend
             var itemsToRemove = MediaItems.Where(x => x.IsSelected).ToList();
             foreach(var item in itemsToRemove)
             {
                 MediaItems.Remove(item);
             }
             _logger.LogInformation("Deleted {Count} media items", selectedCount);
         }
    }

    [RelayCommand]
    private async Task CleanOldMedia()
    {
        if (await _dialogService.ShowConfirmationAsync("Clean Old Media", "Are you sure you want to delete media older than 30 days?"))
        {
             await _dialogService.ShowInformationAsync("Clean Up", "Old media clean up initiated.");
        }
    }
}

/// <summary>
/// View model for individual media items.
/// </summary>
public partial class GameMediaItemViewModel : ObservableObject
{
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

    [RelayCommand]
    private void View()
    {
        // TODO: Open media viewer
    }

    [RelayCommand]
    private void Copy()
    {
        // TODO: Copy to clipboard
    }

    [RelayCommand]
    private void Delete()
    {
        // TODO: Delete this media
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
