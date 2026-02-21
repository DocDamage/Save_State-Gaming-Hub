using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Presentation.Utilities;
// Use ValueObjects as canonical type for ambiguous MugenDiscoveryItem
using MugenDiscoveryItem = SaveState.Core.Mugen.ValueObjects.MugenDiscoveryItem;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

/// <summary>
/// View model for the MUGEN downloads section with throttled search.
/// </summary>
public partial class MugenDownloadsViewModel : MugenSectionViewModelBase, IDisposable
{
    private readonly IMugenDiscoveryService _discoveryService;
    private readonly AsyncSearchThrottleHelper _searchThrottleHelper;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready to search.";

    [ObservableProperty]
    private double _downloadProgress;

    public ObservableCollection<MugenDiscoveryItem> SearchResults { get; } = new();

    public MugenDownloadsViewModel(IMugenDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
        Title = "ASSET DOWNLOADER";

        // Initialize throttled search with 500ms delay for network requests
        _searchThrottleHelper = new AsyncSearchThrottleHelper(
            async (query, ct) =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusMessage = "Ready to search.";
                    });
                    return;
                }

                await ExecuteSearchAsync(query, ct);
            },
            TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Called when SearchQuery changes. Uses throttling to prevent excessive network requests.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        _searchThrottleHelper.UpdateSearchText(value);
    }

    /// <summary>
    /// Executes the search (command or automatic from throttling).
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        await _searchThrottleHelper.SearchImmediatelyAsync(SearchQuery);
    }

    /// <summary>
    /// Internal search execution with cancellation support.
    /// </summary>
    private async Task ExecuteSearchAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = true;
                StatusMessage = "Searching...";
                SearchResults.Clear();
            });

            var result = await _discoveryService.SearchAsync(query, cancellationToken);
            
            cancellationToken.ThrowIfCancellationRequested();

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
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
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when search is cancelled due to new input
            throw;
        }
        catch (Exception ex)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Error: {ex.Message}";
            });
        }
        finally
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = false;
            });
        }
    }

    [RelayCommand]
    private async Task InstallItemAsync(MugenDiscoveryItem item)
    {
        if (item == null) return;

        try
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = true;
                StatusMessage = $"Installing {item.Name}...";
                DownloadProgress = 0;
            });

            var result = await _discoveryService.InstallAsync(item);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.IsSuccess)
                {
                    StatusMessage = $"Successfully installed {item.Name}!";
                    DownloadProgress = 100;
                }
                else
                {
                    StatusMessage = $"Installation failed: {result.Error}";
                }
            });
        }
        catch (Exception ex)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusMessage = $"Error: {ex.Message}";
            });
        }
        finally
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBusy = false;
            });
        }
    }

    /// <summary>
    /// Disposes resources used by this view model.
    /// </summary>
    public void Dispose()
    {
        _searchThrottleHelper.Dispose();
    }
}
