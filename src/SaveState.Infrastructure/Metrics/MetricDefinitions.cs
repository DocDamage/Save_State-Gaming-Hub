using System.Diagnostics.Metrics;

namespace SaveState.Infrastructure.Metrics;

/// <summary>
/// Centralized metric definitions for SaveStateReborn.
/// Uses System.Diagnostics.Metrics for OpenTelemetry compatibility.
/// </summary>
public static class MetricDefinitions
{
    private static readonly Meter _meter = new("SaveStateReborn", "2.5.1");

    #region Business Metrics

    /// <summary>
    /// Counter for games launched.
    /// </summary>
    public static readonly Counter<long> GamesLaunched = _meter.CreateCounter<long>(
        "games.launched",
        description: "Total number of games launched");

    /// <summary>
    /// Counter for save states created.
    /// </summary>
    public static readonly Counter<long> SaveStatesCreated = _meter.CreateCounter<long>(
        "saves.created",
        description: "Total number of save states created");

    /// <summary>
    /// Counter for save states loaded.
    /// </summary>
    public static readonly Counter<long> SaveStatesLoaded = _meter.CreateCounter<long>(
        "saves.loaded",
        description: "Total number of save states loaded");

    /// <summary>
    /// Counter for memory patterns detected.
    /// </summary>
    public static readonly Counter<long> MemoryPatternsDetected = _meter.CreateCounter<long>(
        "memory.patterns.detected",
        description: "Total number of memory patterns detected");

    /// <summary>
    /// Counter for successful auto-discovery operations.
    /// </summary>
    public static readonly Counter<long> AutoDiscoverySuccess = _meter.CreateCounter<long>(
        "autodiscovery.success",
        description: "Number of successful auto-discovery operations");

    /// <summary>
    /// Counter for cheat tables imported.
    /// </summary>
    public static readonly Counter<long> CheatTablesImported = _meter.CreateCounter<long>(
        "cheattables.imported",
        description: "Number of cheat tables imported");

    #endregion

    #region Performance Metrics

    /// <summary>
    /// Histogram for memory scan duration in milliseconds.
    /// </summary>
    public static readonly Histogram<double> MemoryScanDuration = _meter.CreateHistogram<double>(
        "memory.scan.duration",
        unit: "ms",
        description: "Memory scan duration in milliseconds");

    /// <summary>
    /// Histogram for pattern detection duration in milliseconds.
    /// </summary>
    public static readonly Histogram<double> PatternDetectionDuration = _meter.CreateHistogram<double>(
        "pattern.detection.duration",
        unit: "ms",
        description: "Pattern detection duration in milliseconds");

    /// <summary>
    /// Histogram for game launch duration in milliseconds.
    /// </summary>
    public static readonly Histogram<double> GameLaunchDuration = _meter.CreateHistogram<double>(
        "game.launch.duration",
        unit: "ms",
        description: "Game launch duration in milliseconds");

    #endregion

    #region System Metrics

    /// <summary>
    /// Observable gauge for CPU usage percentage.
    /// </summary>
    public static ObservableGauge<double>? CpuUsage { get; private set; }

    /// <summary>
    /// Observable gauge for memory usage in bytes.
    /// </summary>
    public static ObservableGauge<long>? MemoryUsage { get; private set; }

    /// <summary>
    /// Observable gauge for disk usage in bytes.
    /// </summary>
    public static ObservableGauge<long>? DiskUsage { get; private set; }

    /// <summary>
    /// Observable gauge for active sessions.
    /// </summary>
    public static ObservableGauge<long>? ActiveSessions { get; private set; }

    /// <summary>
    /// Observable gauge for attached processes.
    /// </summary>
    public static ObservableGauge<long>? AttachedProcesses { get; private set; }

    /// <summary>
    /// Initializes system gauges with value providers.
    /// </summary>
    public static void InitializeSystemGauges(
        Func<double> cpuProvider,
        Func<long> memoryProvider,
        Func<long> diskProvider,
        Func<long> sessionsProvider,
        Func<long> processesProvider)
    {
        CpuUsage = _meter.CreateObservableGauge(
            "system.cpu.usage",
            cpuProvider,
            unit: "%",
            description: "CPU usage percentage");

        MemoryUsage = _meter.CreateObservableGauge(
            "system.memory.usage",
            memoryProvider,
            unit: "bytes",
            description: "Memory usage in bytes");

        DiskUsage = _meter.CreateObservableGauge(
            "system.disk.usage",
            diskProvider,
            unit: "bytes",
            description: "Disk usage in bytes");

        ActiveSessions = _meter.CreateObservableGauge(
            "sessions.active",
            sessionsProvider,
            description: "Number of active user sessions");

        AttachedProcesses = _meter.CreateObservableGauge(
            "processes.attached",
            processesProvider,
            description: "Number of attached game processes");
    }

    #endregion

    #region Error Metrics

    /// <summary>
    /// Counter for total errors.
    /// </summary>
    public static readonly Counter<long> ErrorsTotal = _meter.CreateCounter<long>(
        "errors.total",
        description: "Total number of errors");

    /// <summary>
    /// Counter for total warnings.
    /// </summary>
    public static readonly Counter<long> WarningsTotal = _meter.CreateCounter<long>(
        "warnings.total",
        description: "Total number of warnings");

    #endregion
}
