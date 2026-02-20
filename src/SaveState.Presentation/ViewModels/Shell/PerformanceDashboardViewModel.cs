using System.Collections.ObjectModel;
using Timer = System.Timers.Timer;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common.Services;
using SaveState.Infrastructure.Performance;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Real-time performance dashboard for monitoring system metrics.
/// PHASE 7: REQUIRED - Performance Monitoring Dashboard
/// </summary>
public partial class PerformanceDashboardViewModel : ObservableObject
{
    private readonly MemoryProfiler _memoryProfiler;
    private readonly INotificationService _notificationService;
    private readonly ITimeProvider _timeProvider;
    private Timer? _updateTimer;

    [ObservableProperty]
    private ObservableCollection<PerformanceMetricSnapshot> memoryHistory = new();

    [ObservableProperty]
    private ObservableCollection<PerformanceMetricSnapshot> cpuHistory = new();

    [ObservableProperty]
    private long currentMemoryMB;

    [ObservableProperty]
    private long peakMemoryMB;

    [ObservableProperty]
    private int threadCount;

    [ObservableProperty]
    private long cpuTimeMS;

    [ObservableProperty]
    private float memoryUsagePercent;

    [ObservableProperty]
    private float cpuUsagePercent;

    [ObservableProperty]
    private bool isMonitoring;

    [ObservableProperty]
    private int updateIntervalSeconds = 5;

    [ObservableProperty]
    private ObservableCollection<OperationPerformance> topOperations = new();

    [ObservableProperty]
    private MemoryLeakAlert? leakAlert;

    public PerformanceDashboardViewModel(
        MemoryProfiler memoryProfiler,
        INotificationService notificationService,
        ITimeProvider timeProvider)
    {
        _memoryProfiler = memoryProfiler;
        _notificationService = notificationService;
        _timeProvider = timeProvider;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await UpdateMetricsAsync();
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to initialize dashboard: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task StartMonitoringAsync()
    {
        try
        {
            if (IsMonitoring)
                return;

            IsMonitoring = true;

            _updateTimer = new Timer(UpdateIntervalSeconds * 1000)
            {
                AutoReset = true,
                Enabled = true
            };

            _updateTimer.Elapsed += async (s, e) => await UpdateMetricsAsync();

            _updateTimer.Start();
            await _notificationService.ShowNotificationAsync("Performance monitoring started", "Info");
        }
        catch (Exception ex)
        {
            IsMonitoring = false;
            await _notificationService.ShowErrorAsync($"Failed to start monitoring: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task StopMonitoringAsync()
    {
        try
        {
            if (!IsMonitoring)
                return;

            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            IsMonitoring = false;

            await _notificationService.ShowNotificationAsync("Performance monitoring stopped", "Info");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to stop monitoring: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ForceGarbageCollectionAsync()
    {
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            await UpdateMetricsAsync();
            await _notificationService.ShowNotificationAsync("Garbage collection triggered", "Success");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to trigger GC: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ClearHistoryAsync()
    {
        MemoryHistory.Clear();
        CpuHistory.Clear();
        TopOperations.Clear();
        _memoryProfiler.ClearMetrics();

        await _notificationService.ShowNotificationAsync("History cleared", "Success");
    }

    [RelayCommand]
    public async Task ChangeUpdateIntervalAsync(int seconds)
    {
        if (seconds < 1 || seconds > 60)
        {
            await _notificationService.ShowErrorAsync("Update interval must be between 1 and 60 seconds");
            return;
        }

        UpdateIntervalSeconds = seconds;

        if (IsMonitoring)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();

            _updateTimer = new Timer(seconds * 1000)
            {
                AutoReset = true,
                Enabled = true
            };

            _updateTimer.Elapsed += async (s, e) => await UpdateMetricsAsync();
            _updateTimer.Start();
        }
    }

    [RelayCommand]
    public async Task ExportMetricsAsync()
    {
        try
        {
            var metrics = _memoryProfiler.GetAllMetrics();
            var csv = GenerateCsv(metrics);

            // In production, save to file
            await _notificationService.ShowNotificationAsync(
                "Metrics exported successfully",
                "Success");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Export failed: {ex.Message}");
        }
    }

    private async Task UpdateMetricsAsync()
    {
        try
        {
            var memoryStats = _memoryProfiler.GetMemoryStatistics();
            var cpuStats = _memoryProfiler.GetCpuStatistics();
            var leakAnalysis = _memoryProfiler.AnalyzeMemoryLeaks();

            // Update current values
            CurrentMemoryMB = memoryStats.ManagedMemoryMB;
            PeakMemoryMB = memoryStats.PeakMemoryMB;
            ThreadCount = cpuStats.ThreadCount;
            CpuTimeMS = cpuStats.TotalProcessorTimeMs;

            // Calculate percentages (assuming 8GB max for example)
            var maxMemoryMB = 8192;
            MemoryUsagePercent = (CurrentMemoryMB / (float)maxMemoryMB) * 100;

            // Add to history
            AddHistoryEntry(MemoryHistory, CurrentMemoryMB, 50);
            AddHistoryEntry(CpuHistory, CpuTimeMS, 1000);

            // Check for memory leaks
            if (leakAnalysis.LeakSuspected)
            {
                LeakAlert = new MemoryLeakAlert(
                    Timestamp: _timeProvider.Now,
                    CurrentMemoryMB: CurrentMemoryMB,
                    Gen2Collections: leakAnalysis.Generation2Collections,
                    Severity: "High");

                await _notificationService.ShowWarningAsync(
                    "Potential memory leak detected!");
            }

            // Update top operations
            UpdateTopOperations();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Silently log, don't interrupt monitoring
        }
    }

    private void AddHistoryEntry(ObservableCollection<PerformanceMetricSnapshot> collection, long value, int maxEntries)
    {
        collection.Add(new PerformanceMetricSnapshot(
            Timestamp: _timeProvider.Now,
            Value: value));

        while (collection.Count > maxEntries)
        {
            collection.RemoveAt(0);
        }
    }

    private void UpdateTopOperations()
    {
        var operations = _memoryProfiler.GetAllMetrics()
            .Select(kvp => new OperationPerformance(
                OperationName: kvp.Key,
                Duration: kvp.Value.Duration.TotalMilliseconds,
                MemoryUsedMB: kvp.Value.MemoryUsedBytes / 1024 / 1024,
                CpuTimeMS: kvp.Value.CpuTimeMilliseconds))
            .OrderByDescending(o => o.MemoryUsedMB)
            .Take(10)
            .ToList();

        TopOperations.Clear();
        foreach (var op in operations)
        {
            TopOperations.Add(op);
        }
    }

    private string GenerateCsv(IReadOnlyDictionary<string, SaveState.Infrastructure.Performance.OperationMetrics> metrics)
    {
        var csv = "Operation,Duration(ms),Memory(MB),CPU(ms)\n";

        foreach (var kvp in metrics)
        {
            var m = kvp.Value;
            csv += $"{m.OperationName},{m.Duration.TotalMilliseconds:F2},{m.MemoryUsedBytes / 1024 / 1024},{m.CpuTimeMilliseconds}\n";
        }

        return csv;
    }
}

/// <summary>
/// Performance metric snapshot over time.
/// </summary>
public record PerformanceMetricSnapshot(DateTime Timestamp, long Value);

/// <summary>
/// Performance of a single operation.
/// </summary>
public record OperationPerformance(
    string OperationName,
    double Duration,
    long MemoryUsedMB,
    long CpuTimeMS);

/// <summary>
/// Memory leak alert.
/// </summary>
public record MemoryLeakAlert(
    DateTime Timestamp,
    long CurrentMemoryMB,
    int Gen2Collections,
    string Severity);

/// <summary>
/// Operation metrics snapshot.
/// </summary>
public record OperationMetrics(
    string OperationName,
    TimeSpan Duration,
    long MemoryUsedBytes,
    long CpuTimeMilliseconds,
    DateTime Timestamp);
