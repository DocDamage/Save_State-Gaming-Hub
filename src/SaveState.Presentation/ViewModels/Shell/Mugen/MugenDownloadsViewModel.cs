using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

public partial class MugenDownloadsViewModel : MugenSectionViewModelBase
{
    private readonly IMugenDiscoveryService _discoveryService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready to search.";

    // Kept for compatibility if XAML binds to it, but mainly superseded by Discovery logic
    [ObservableProperty]
    private string _assetUrl = string.Empty;

    [ObservableProperty]
    private double _downloadProgress;

    public ObservableCollection<MugenDiscoveryItem> SearchResults { get; } = new();

    public MugenDownloadsViewModel(IMugenDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
        Title = "ASSET DOWNLOADER";
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Searching...";
            SearchResults.Clear();

            var result = await _discoveryService.SearchAsync(SearchQuery);
            if (result.IsSuccess && result.Value != null)
            {
                foreach (var item in result.Value)
                {
                    SearchResults.Add(item);
                }
                StatusMessage = $"Found {SearchResults.Count} items.";
            }
            else
            {
                StatusMessage = result.Error ?? "Search failed.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task InstallItemAsync(MugenDiscoveryItem item)
    {
        if (item == null) return;

        try
        {
            IsBusy = true;
            StatusMessage = $"Installing {item.Name}...";
            DownloadProgress = 0; // If service supported progress, we'd hook it up

            var result = await _discoveryService.InstallAsync(item);

            if (result.IsSuccess)
            {
                StatusMessage = $"Successfully installed {item.Name}!";
                DownloadProgress = 100;
            }
            else
            {
                StatusMessage = $"Installation failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Legacy method from placeholder, kept or refactored
    [RelayCommand]
    private async Task DownloadAssetAsync()
    {
        // ... (Logic for direct URL download if needed, skipping for now as Discovery is primary)
        await SearchAsync();
    }
}
