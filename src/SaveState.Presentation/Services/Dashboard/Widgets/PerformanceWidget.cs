using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SaveState.Core.Performance.Services;
using System;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Widget showing live performance data (FPS, CPU, GPU).
/// </summary>
public partial class PerformanceWidget : WidgetBase
{
    private readonly IPerformanceMonitor _performanceMonitor;

    public PerformanceWidget(IPerformanceMonitor performanceMonitor, ILogger<PerformanceWidget> logger)
        : base(logger)
    {
        _performanceMonitor = performanceMonitor;
        _performanceMonitor.SnapshotUpdated += (s, snapshot) =>
        {
            Fps = snapshot.Fps;
            CpuUsage = snapshot.CpuUsagePercent;
            GpuUsage = snapshot.GpuUsagePercent;
            RamUsage = snapshot.RamUsageMb;
        };
    }

    public override string Id => "performance-live";
    public override string Title => "Live Performance";
    public override string Icon => "⚡";
    public override WidgetSize DefaultSize => WidgetSize.Small;
    public override WidgetSize[] SupportedSizes => new[] { WidgetSize.Small, WidgetSize.Medium };

    [ObservableProperty]
    private float _fps;

    [ObservableProperty]
    private float _cpuUsage;

    [ObservableProperty]
    private float _gpuUsage;

    [ObservableProperty]
    private long _ramUsage;

    protected override async Task LoadDataAsync()
    {
        var snapshot = _performanceMonitor.GetCurrentSnapshot();
        if (snapshot != null)
        {
            Fps = snapshot.Fps;
            CpuUsage = snapshot.CpuUsagePercent;
            GpuUsage = snapshot.GpuUsagePercent;
            RamUsage = snapshot.RamUsageMb;
        }
        else if (!_performanceMonitor.IsMonitoring)
        {
             // If not monitoring a specific process, maybe show global usage if IPerformanceMonitor supports it
             // For now, use mock global data
             Fps = 0;
             CpuUsage = 5.0f; // Mock system idle
             GpuUsage = 2.0f;
             RamUsage = 1024;
        }

        await Task.CompletedTask;
    }
}
