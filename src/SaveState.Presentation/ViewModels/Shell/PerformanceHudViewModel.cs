using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Timers;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the performance HUD overlay.
/// </summary>
public partial class PerformanceHudViewModel : ObservableObject, IDisposable
{
    private readonly IOverlayService _overlayService;
    private readonly System.Timers.Timer _updateTimer;

    private int _cpuUsage;
    private int _gpuUsage;
    private long _memoryUsage;
    private int _fps;

    public PerformanceHudViewModel(IOverlayService overlayService)
    {
        _overlayService = overlayService;

        // Update performance stats every second
        _updateTimer = new System.Timers.Timer(1000);
        _updateTimer.Elapsed += OnUpdateTimerElapsed;
        _updateTimer.Start();

        // Initial update
        UpdateStats();
    }

    /// <summary>
    /// Gets the CPU usage percentage.
    /// </summary>
    public int CpuUsage
    {
        get => _cpuUsage;
        private set => SetProperty(ref _cpuUsage, value);
    }

    /// <summary>
    /// Gets the GPU usage percentage.
    /// </summary>
    public int GpuUsage
    {
        get => _gpuUsage;
        private set => SetProperty(ref _gpuUsage, value);
    }

    /// <summary>
    /// Gets the memory usage in MB.
    /// </summary>
    public long MemoryUsage
    {
        get => _memoryUsage;
        private set => SetProperty(ref _memoryUsage, value);
    }

    /// <summary>
    /// Gets the current FPS.
    /// </summary>
    public int Fps
    {
        get => _fps;
        private set => SetProperty(ref _fps, value);
    }

    /// <summary>
    /// Command to close the performance HUD.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _overlayService.HidePerformanceHudOverlay();
    }

    private void OnUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateStats();
    }

    private void UpdateStats()
    {
        // TODO: Get real performance data from IPerformanceMonitor
        // For now, simulate some data
        CpuUsage = new Random().Next(10, 90);
        GpuUsage = new Random().Next(20, 95);
        MemoryUsage = new Random().Next(1000, 8000);
        Fps = new Random().Next(30, 144);
    }

    /// <summary>
    /// Disposes the view model.
    /// </summary>
    public void Dispose()
    {
        _updateTimer?.Dispose();
    }
}