using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

public partial class MugenDownloadsViewModel : MugenSectionViewModelBase
{
    [ObservableProperty]
    private string _assetUrl = string.Empty;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _downloadStatus = "Ready";

    public MugenDownloadsViewModel()
    {
        Title = "ASSET DOWNLOADER";
    }

    [RelayCommand]
    private async Task DownloadAssetAsync()
    {
        if (string.IsNullOrWhiteSpace(AssetUrl)) return;

        try
        {
            IsDownloading = true;
            DownloadStatus = "Connecting...";
            DownloadProgress = 0;

            // Simulate download
            for (int i = 0; i <= 100; i += 10)
            {
                await Task.Delay(200);
                DownloadProgress = i;
                DownloadStatus = $"Downloading... {i}%";
            }

            DownloadStatus = "Installation Complete!";
            AssetUrl = string.Empty;
        }
        catch (Exception ex)
        {
            DownloadStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }
}
