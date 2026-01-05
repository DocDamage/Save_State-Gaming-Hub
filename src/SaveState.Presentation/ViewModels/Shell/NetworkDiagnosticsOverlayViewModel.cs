using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class NetworkDiagnosticsOverlayViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;

    [ObservableProperty]
    private string _connectionStatus = "🟢 Connected";

    [ObservableProperty]
    private int _ping = 45;

    [ObservableProperty]
    private string _downloadSpeed = "125 Mbps";

    [ObservableProperty]
    private string _uploadSpeed = "45 Mbps";

    [ObservableProperty]
    private int _packetLoss = 0;

    [ObservableProperty]
    private ObservableCollection<NetworkEndpointViewModel> _endpoints = new();

    public NetworkDiagnosticsOverlayViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;
        LoadNetworkEndpoints();
    }

    private void LoadNetworkEndpoints()
    {
        Endpoints.Clear();
        Endpoints.Add(new NetworkEndpointViewModel("Steam API", "🟢 Online", "12ms"));
        Endpoints.Add(new NetworkEndpointViewModel("IGDB API", "🟢 Online", "34ms"));
        Endpoints.Add(new NetworkEndpointViewModel("RetroAchievements", "🟢 Online", "56ms"));
        Endpoints.Add(new NetworkEndpointViewModel("Google Drive", "🟡 Slow", "234ms"));
        Endpoints.Add(new NetworkEndpointViewModel("Discord RPC", "🟢 Online", "23ms"));
    }

    [RelayCommand]
    private void RunDiagnostics()
    {
        // Simulate running diagnostics
        LoadNetworkEndpoints();
    }

    [RelayCommand]
    private void Close()
    {
        _overlayService.HideNetworkDiagnosticsOverlay();
    }
}

public record NetworkEndpointViewModel(
    string Name,
    string Status,
    string Latency);
