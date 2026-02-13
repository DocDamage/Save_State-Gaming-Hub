using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Performance.Services;
using SaveState.Presentation.Services;
using System.Timers;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the performance HUD overlay.
/// </summary>
public partial class PerformanceHudViewModel : ObservableObject, IDisposable
{
    private readonly IOverlayService _overlayService;
    private readonly IPerformanceMonitor? _performanceMonitor;
    private readonly System.Timers.Timer _updateTimer;

    private int _cpuUsage;
    private int _gpuUsage;
    private long _memoryUsage;
    private int _fps;
    private float _frameTimeMs;
    private float? _cpuTemp;
    private float? _gpuTemp;

    public PerformanceHudViewModel(
        IOverlayService overlayService,
        IPerformanceMonitor? performanceMonitor)
    {
        _overlayService = overlayService;
        _performanceMonitor = performanceMonitor;

        // Subscribe to performance updates if monitor is available
        if (_performanceMonitor != null)
        {
            _performanceMonitor.SnapshotUpdated += OnSnapshotUpdated;
        }

        // Fallback: Update performance stats every second
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
    /// Gets the frame time in milliseconds.
    /// </summary>
    public float FrameTimeMs
    {
        get => _frameTimeMs;
        private set => SetProperty(ref _frameTimeMs, value);
    }

    /// <summary>
    /// Gets the CPU temperature (if available).
    /// </summary>
    public float? CpuTemp
    {
        get => _cpuTemp;
        private set => SetProperty(ref _cpuTemp, value);
    }

    /// <summary>
    /// Gets the GPU temperature (if available).
    /// </summary>
    public float? GpuTemp
    {
        get => _gpuTemp;
        private set => SetProperty(ref _gpuTemp, value);
    }

    /// <summary>
    /// Command to close the performance HUD.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        _overlayService.HidePerformanceHudOverlay();
    }

    private void OnSnapshotUpdated(object? sender, PerformanceSnapshot snapshot)
    {
        // Update from real performance data
        CpuUsage = (int)snapshot.CpuUsagePercent;
        GpuUsage = snapshot.GpuUsagePercent.HasValue ? (int)snapshot.GpuUsagePercent.Value : 0;
        MemoryUsage = snapshot.RamUsageMb;
        Fps = (int)snapshot.Fps;
        FrameTimeMs = snapshot.FrameTimeMs;
        CpuTemp = snapshot.CpuTempCelsius;
        GpuTemp = snapshot.GpuTempCelsius;
    }

    private void OnUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateStats();
    }

    private void UpdateStats()
    {
        // Try to get real performance data
        var snapshot = _performanceMonitor?.GetCurrentSnapshot();
        if (snapshot != null)
        {
            OnSnapshotUpdated(this, snapshot);
        }
        else
        {
            // Fallback to system memory usage
            var process = System.Diagnostics.Process.GetCurrentProcess();
            MemoryUsage = process.WorkingSet64 / (1024 * 1024); // Convert to MB
        }
    }

    /// <summary>
    /// Disposes the view model.
    /// </summary>
    public void Dispose()
    {
        _updateTimer?.Dispose();
        if (_performanceMonitor != null)
        {
            _performanceMonitor.SnapshotUpdated -= OnSnapshotUpdated;
        }
    }
}
