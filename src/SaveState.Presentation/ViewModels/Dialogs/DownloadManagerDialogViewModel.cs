// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.WebBrowser.Models;
using SaveState.Core.WebBrowser.Services;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the download manager dialog.
/// </summary>
public sealed partial class DownloadManagerDialogViewModel : ObservableObject
{
    private readonly IBrowserService _browserService;
    private readonly ILogger<DownloadManagerDialogViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private ObservableCollection<DownloadItemViewModel> _activeDownloads = new();

    [ObservableProperty]
    private ObservableCollection<DownloadItemViewModel> _completedDownloads = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DownloadItemViewModel? _selectedDownload;

    public ObservableCollection<DownloadItemViewModel> FilteredActiveDownloads => new(
        ActiveDownloads.Where(d =>
            string.IsNullOrWhiteSpace(SearchQuery) ||
            d.FileName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)));

    public ObservableCollection<DownloadItemViewModel> FilteredCompletedDownloads => new(
        CompletedDownloads.Where(d =>
            string.IsNullOrWhiteSpace(SearchQuery) ||
            d.FileName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)));

    public int ActiveDownloadCount => ActiveDownloads.Count(d => d.State == DownloadState.InProgress);
    public int CompletedDownloadCount => CompletedDownloads.Count;

    public DownloadManagerDialogViewModel(
        IBrowserService browserService,
        ILogger<DownloadManagerDialogViewModel> logger,
        ITimeProvider? timeProvider = null)
    {
        _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? SystemTimeProvider.Instance;

        // Subscribe to download events
        _browserService.DownloadStarted += OnDownloadStarted;
        _browserService.DownloadProgressChanged += OnDownloadProgressChanged;
        _browserService.DownloadCompleted += OnDownloadCompleted;

        _ = LoadDownloadsAsync();
    }

    private void OnDownloadStarted(object? sender, BrowserDownload download)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            var viewModel = new DownloadItemViewModel(download);
            ActiveDownloads.Add(viewModel);
            OnPropertyChanged(nameof(FilteredActiveDownloads));
            OnPropertyChanged(nameof(ActiveDownloadCount));
        });
    }

    private void OnDownloadProgressChanged(object? sender, BrowserDownload download)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            var existing = ActiveDownloads.FirstOrDefault(d => d.Id == download.Id);
            existing?.UpdateFrom(download);
        });
    }

    private void OnDownloadCompleted(object? sender, BrowserDownload download)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            var existing = ActiveDownloads.FirstOrDefault(d => d.Id == download.Id);
            if (existing != null)
            {
                ActiveDownloads.Remove(existing);
                existing.UpdateFrom(download);
                CompletedDownloads.Insert(0, existing);

                OnPropertyChanged(nameof(FilteredActiveDownloads));
                OnPropertyChanged(nameof(FilteredCompletedDownloads));
                OnPropertyChanged(nameof(ActiveDownloadCount));
                OnPropertyChanged(nameof(CompletedDownloadCount));
            }
        });
    }

    [RelayCommand]
    private async Task LoadDownloadsAsync()
    {
        try
        {
            var activeDownloads = new List<DownloadItemViewModel>();
            var completedDownloads = new List<DownloadItemViewModel>();

            await Task.Run(() =>
            {
                foreach (var download in _browserService.Downloads)
                {
                    var viewModel = new DownloadItemViewModel(download);

                    if (download.State == DownloadState.InProgress)
                    {
                        activeDownloads.Add(viewModel);
                    }
                    else
                    {
                        completedDownloads.Add(viewModel);
                    }
                }
            });

            ActiveDownloads.Clear();
            CompletedDownloads.Clear();

            foreach (var vm in activeDownloads)
            {
                ActiveDownloads.Add(vm);
            }

            foreach (var vm in completedDownloads)
            {
                CompletedDownloads.Add(vm);
            }

            OnPropertyChanged(nameof(FilteredActiveDownloads));
            OnPropertyChanged(nameof(FilteredCompletedDownloads));
            OnPropertyChanged(nameof(ActiveDownloadCount));
            OnPropertyChanged(nameof(CompletedDownloadCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load downloads");
        }
    }

    [RelayCommand]
    private async Task PauseDownloadAsync(DownloadItemViewModel? download)
    {
        if (download == null) return;

        try
        {
            var result = await _browserService.PauseDownloadAsync(download.Id);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to pause download: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause download");
        }
    }

    [RelayCommand]
    private async Task ResumeDownloadAsync(DownloadItemViewModel? download)
    {
        if (download == null) return;

        try
        {
            var result = await _browserService.ResumeDownloadAsync(download.Id);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to resume download: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume download");
        }
    }

    [RelayCommand]
    private async Task CancelDownloadAsync(DownloadItemViewModel? download)
    {
        if (download == null) return;

        try
        {
            var result = await _browserService.CancelDownloadAsync(download.Id);
            if (result.IsSuccess)
            {
                ActiveDownloads.Remove(download);
                OnPropertyChanged(nameof(FilteredActiveDownloads));
                OnPropertyChanged(nameof(ActiveDownloadCount));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel download");
        }
    }

    [RelayCommand]
    private void OpenFileAsync(DownloadItemViewModel? download)
    {
        if (download?.SavePath == null) return;

        try
        {
            if (File.Exists(download.SavePath))
            {
                Process.Start(new ProcessStartInfo(download.SavePath) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file");
        }
    }

    [RelayCommand]
    private void ShowInFolderAsync(DownloadItemViewModel? download)
    {
        if (download?.SavePath == null) return;

        try
        {
            var folder = Path.GetDirectoryName(download.SavePath);
            if (folder != null && Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show file in folder");
        }
    }

    [RelayCommand]
    private async Task ClearCompletedAsync()
    {
        try
        {
            var result = await _browserService.ClearCompletedDownloadsAsync();
            if (result.IsSuccess)
            {
                CompletedDownloads.Clear();
                OnPropertyChanged(nameof(FilteredCompletedDownloads));
                OnPropertyChanged(nameof(CompletedDownloadCount));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear completed downloads");
        }
    }

    [RelayCommand]
    private void CloseDialog()
    {
        // Unsubscribe from events
        _browserService.DownloadStarted -= OnDownloadStarted;
        _browserService.DownloadProgressChanged -= OnDownloadProgressChanged;
        _browserService.DownloadCompleted -= OnDownloadCompleted;

        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredActiveDownloads));
        OnPropertyChanged(nameof(FilteredCompletedDownloads));
    }
}

/// <summary>
/// ViewModel for a single download item.
/// </summary>
public sealed partial class DownloadItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string? _mimeType;

    [ObservableProperty]
    private long _totalBytes;

    [ObservableProperty]
    private long _receivedBytes;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private DownloadState _state;

    [ObservableProperty]
    private string? _savePath;

    [ObservableProperty]
    private DateTime _startedAt;

    [ObservableProperty]
    private DateTime? _completedAt;

    [ObservableProperty]
    private string _downloadSpeed = string.Empty;

    [ObservableProperty]
    private string _remainingTime = string.Empty;

    public string FormattedTotalSize => FormatBytes(TotalBytes);
    public string FormattedReceivedSize => FormatBytes(ReceivedBytes);

    public string StatusText => State switch
    {
        DownloadState.InProgress => $"{Progress:P0} - {DownloadSpeed}",
        DownloadState.Completed => "Completed",
        DownloadState.Canceled => "Canceled",
        DownloadState.Failed => "Failed",
        _ => "Unknown"
    };

    public bool CanPause => State == DownloadState.InProgress;
    public bool CanResume => State == DownloadState.InProgress;
    public bool CanCancel => State == DownloadState.InProgress;
    public bool CanOpen => State == DownloadState.Completed && SavePath != null && File.Exists(SavePath);

    public DownloadItemViewModel(BrowserDownload download)
    {
        UpdateFrom(download);
    }

    public void UpdateFrom(BrowserDownload download)
    {
        Id = download.Id;
        FileName = download.FileName;
        Url = download.Url;
        MimeType = download.MimeType;
        TotalBytes = download.TotalBytes;
        ReceivedBytes = download.ReceivedBytes;
        Progress = download.Progress;
        State = download.State;
        SavePath = download.SavePath;
        StartedAt = download.StartedAt;
        CompletedAt = download.CompletedAt;

        // Calculate speed (simplified)
        if (State == DownloadState.InProgress)
        {
            var timeProvider = SystemTimeProvider.Instance;
            var elapsed = timeProvider.Now - StartedAt;
            if (elapsed.TotalSeconds > 0 && ReceivedBytes > 0)
            {
                var bytesPerSecond = ReceivedBytes / elapsed.TotalSeconds;
                DownloadSpeed = $"{FormatBytes((long)bytesPerSecond)}/s";

                if (bytesPerSecond > 0 && TotalBytes > 0)
                {
                    var remainingBytes = TotalBytes - ReceivedBytes;
                    var remainingSeconds = remainingBytes / bytesPerSecond;
                    RemainingTime = FormatTimeSpan(TimeSpan.FromSeconds(remainingSeconds));
                }
            }
        }

        OnPropertyChanged(nameof(FormattedTotalSize));
        OnPropertyChanged(nameof(FormattedReceivedSize));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanResume));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(CanOpen));
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1) return $"{timeSpan.TotalHours:F0}h {timeSpan.Minutes}m";
        if (timeSpan.TotalMinutes >= 1) return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        return $"{timeSpan.Seconds}s";
    }
}
