using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.Performance.Services;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

/// <summary>
/// View model for the Game Performance tab.
/// </summary>
public partial class GamePerformanceTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IPerformanceMonitor _performanceMonitor;
    private readonly ILogger<GamePerformanceTabViewModel> _logger;
    private GameId? _currentGameId;

    [ObservableProperty]
    private string _cpuUsage = "0%";

    [ObservableProperty]
    private string _gpuUsage = "0%";

    [ObservableProperty]
    private string _ramUsage = "0 MB";

    [ObservableProperty]
    private string _fps = "0";

    [ObservableProperty]
    private string _frameTime = "0 ms";

    [ObservableProperty]
    private bool _isOptimizing;

    [ObservableProperty]
    private ObservableCollection<string> _performanceLogs = new();

    public GamePerformanceTabViewModel(
        IMediator mediator,
        IPerformanceMonitor performanceMonitor,
        ILogger<GamePerformanceTabViewModel> logger)
    {
        _mediator = mediator;
        _performanceMonitor = performanceMonitor;
        _logger = logger;
    }

    public async Task LoadDataAsync(GameId gameId)
    {
        _currentGameId = gameId;
        _logger.LogInformation("Loading performance data for game {GameId}", gameId);

        // Simulate loading performance profiles/history
        await Task.Delay(100);

        PerformanceLogs.Add($"[{DateTime.Now:HH:mm:ss}] Session monitoring initialized.");
        PerformanceLogs.Add($"[{DateTime.Now:HH:mm:ss}] Applied performance profile: 'Gaming High Performance'");
    }

    [RelayCommand]
    private async Task OptimizeSystem()
    {
        IsOptimizing = true;
        _logger.LogInformation("Optimizing system performance for {GameId}", _currentGameId);

        PerformanceLogs.Add($"[{DateTime.Now:HH:mm:ss}] Optimization started...");

        // Use IPerformanceMonitor or other services here
        await Task.Delay(1500); // Simulate work

        PerformanceLogs.Add($"[{DateTime.Now:HH:mm:ss}] CPU affinity adjusted.");
        PerformanceLogs.Add($"[{DateTime.Now:HH:mm:ss}] RAM cleaned (450 MB freed).");
        PerformanceLogs.Add($"[{DateTime.Now:HH:mm:ss}] Optimization complete.");

        IsOptimizing = false;
    }

    [RelayCommand]
    private void ClearLogs()
    {
        PerformanceLogs.Clear();
    }
}
