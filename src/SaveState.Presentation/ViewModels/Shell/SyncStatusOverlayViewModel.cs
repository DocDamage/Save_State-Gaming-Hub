using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class SyncStatusOverlayViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;

    [ObservableProperty]
    private string _currentProvider = "Google Drive";

    [ObservableProperty]
    private string _syncStatus = "🟢 Synced";

    [ObservableProperty]
    private string _lastSyncTime = "2 minutes ago";

    [ObservableProperty]
    private double _syncProgress = 0;

    [ObservableProperty]
    private bool _isSyncing = false;

    [ObservableProperty]
    private string _uploadedFiles = "0";

    [ObservableProperty]
    private string _downloadedFiles = "0";

    [ObservableProperty]
    private string _totalSize = "0 MB";

    [ObservableProperty]
    private ObservableCollection<SyncFileViewModel> _recentFiles = new();

    public SyncStatusOverlayViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;
        LoadRecentFiles();
    }

    private void LoadRecentFiles()
    {
        RecentFiles.Clear();
        RecentFiles.Add(new SyncFileViewModel("game_save_001.sav", "Uploaded", "2.3 MB", "Just now"));
        RecentFiles.Add(new SyncFileViewModel("screenshot_2024.png", "Uploaded", "1.8 MB", "1 min ago"));
        RecentFiles.Add(new SyncFileViewModel("config.json", "Downloaded", "12 KB", "5 mins ago"));
        RecentFiles.Add(new SyncFileViewModel("backup_full.zip", "Uploaded", "45.2 MB", "10 mins ago"));
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        IsSyncing = true;
        SyncProgress = 0;
        SyncStatus = "🟡 Syncing...";

        for (int i = 0; i <= 100; i += 10)
        {
            SyncProgress = i;
            await Task.Delay(200);
        }

        IsSyncing = false;
        SyncStatus = "🟢 Synced";
        LastSyncTime = "Just now";
        LoadRecentFiles();
    }

    [RelayCommand]
    private void ViewConflicts()
    {
        _overlayService.HideSyncStatusOverlay();
        _overlayService.ShowConflictsResolutionOverlay();
    }

    [RelayCommand]
    private void ConfigureProvider()
    {
        _overlayService.HideSyncStatusOverlay();
        _overlayService.ShowProviderConfigurationDialog();
    }

    [RelayCommand]
    private void Close()
    {
        _overlayService.HideSyncStatusOverlay();
    }
}

public record SyncFileViewModel(
    string FileName,
    string Action,
    string Size,
    string TimeAgo);
