using System.Diagnostics.Metrics;

namespace SaveState.Infrastructure.Metrics;

/// <summary>
/// Application-wide metrics using System.Diagnostics.Metrics (OpenTelemetry compatible).
/// </summary>
public static class ApplicationMetrics
{
    private static readonly Meter _meter = new("SaveStateReborn", "1.0.0");

    #region Game Library Metrics

    /// <summary>
    /// Counter for games launched.
    /// </summary>
    public static readonly Counter<long> GamesLaunched = _meter.CreateCounter<long>(
        "games.launched",
        description: "Number of games launched");

    /// <summary>
    /// Counter for games installed.
    /// </summary>
    public static readonly Counter<long> GamesInstalled = _meter.CreateCounter<long>(
        "games.installed",
        description: "Number of games installed");

    /// <summary>
    /// Counter for games imported.
    /// </summary>
    public static readonly Counter<long> GamesImported = _meter.CreateCounter<long>(
        "games.imported",
        description: "Number of games imported from external sources");

    /// <summary>
    /// Histogram for game session duration.
    /// </summary>
    public static readonly Histogram<double> GameSessionDuration = _meter.CreateHistogram<double>(
        "games.session_duration_minutes",
        description: "Game session duration in minutes",
        unit: "minutes");

    #endregion

    #region Save State Metrics

    /// <summary>
    /// Counter for save states created.
    /// </summary>
    public static readonly Counter<long> SaveStatesCreated = _meter.CreateCounter<long>(
        "savestates.created",
        description: "Number of save states created");

    /// <summary>
    /// Counter for save states loaded.
    /// </summary>
    public static readonly Counter<long> SaveStatesLoaded = _meter.CreateCounter<long>(
        "savestates.loaded",
        description: "Number of save states loaded");

    /// <summary>
    /// Counter for save states synced to cloud.
    /// </summary>
    public static readonly Counter<long> SaveStatesSynced = _meter.CreateCounter<long>(
        "savestates.synced",
        description: "Number of save states synced to cloud");

    /// <summary>
    /// Histogram for save state file size.
    /// </summary>
    public static readonly Histogram<long> SaveStateFileSize = _meter.CreateHistogram<long>(
        "savestates.file_size_bytes",
        description: "Save state file size in bytes",
        unit: "bytes");

    #endregion

    #region Cloud Sync Metrics

    /// <summary>
    /// Counter for cloud sync operations.
    /// </summary>
    public static readonly Counter<long> CloudSyncOperations = _meter.CreateCounter<long>(
        "cloudsync.operations",
        description: "Number of cloud sync operations");

    /// <summary>
    /// Counter for cloud sync failures.
    /// </summary>
    public static readonly Counter<long> CloudSyncFailures = _meter.CreateCounter<long>(
        "cloudsync.failures",
        description: "Number of cloud sync failures");

    /// <summary>
    /// Histogram for cloud sync duration.
    /// </summary>
    public static readonly Histogram<double> CloudSyncDuration = _meter.CreateHistogram<double>(
        "cloudsync.duration_seconds",
        description: "Cloud sync operation duration in seconds",
        unit: "seconds");

    #endregion

    #region AI Service Metrics

    /// <summary>
    /// Counter for AI queries.
    /// </summary>
    public static readonly Counter<long> AiQueries = _meter.CreateCounter<long>(
        "ai.queries",
        description: "Number of AI queries");

    /// <summary>
    /// Counter for AI query failures.
    /// </summary>
    public static readonly Counter<long> AiQueryFailures = _meter.CreateCounter<long>(
        "ai.query_failures",
        description: "Number of AI query failures");

    /// <summary>
    /// Histogram for AI query duration.
    /// </summary>
    public static readonly Histogram<double> AiQueryDuration = _meter.CreateHistogram<double>(
        "ai.query_duration_seconds",
        description: "AI query duration in seconds",
        unit: "seconds");

    /// <summary>
    /// Counter for AI tokens used.
    /// </summary>
    public static readonly Counter<long> AiTokensUsed = _meter.CreateCounter<long>(
        "ai.tokens_used",
        description: "Number of AI tokens used");

    #endregion

    #region MUGEN Metrics

    /// <summary>
    /// Counter for MUGEN battles.
    /// </summary>
    public static readonly Counter<long> MugenBattles = _meter.CreateCounter<long>(
        "mugen.battles",
        description: "Number of MUGEN battles");

    /// <summary>
    /// Counter for MUGEN characters imported.
    /// </summary>
    public static readonly Counter<long> MugenCharactersImported = _meter.CreateCounter<long>(
        "mugen.characters_imported",
        description: "Number of MUGEN characters imported");

    #endregion

    #region System Metrics

    /// <summary>
    /// Observable gauge for memory usage.
    /// </summary>
    public static readonly ObservableGauge<long> MemoryUsage = _meter.CreateObservableGauge(
        "system.memory_usage_bytes",
        () => GC.GetTotalMemory(false),
        unit: "bytes",
        description: "Current memory usage in bytes");

    /// <summary>
    /// Observable gauge for GC collections.
    /// </summary>
    public static readonly ObservableGauge<int> GCCollections = _meter.CreateObservableGauge(
        "system.gc_collections_gen0",
        () => GC.CollectionCount(0),
        description: "Number of Gen 0 garbage collections");

    #endregion
}

/// <summary>
/// Helper class for recording metrics with tags.
/// </summary>
public static class MetricsHelper
{
    /// <summary>
    /// Records a game launch with tags.
    /// </summary>
    public static void RecordGameLaunch(string platform, string genre)
    {
        ApplicationMetrics.GamesLaunched.Add(1, 
            new KeyValuePair<string, object?>("platform", platform),
            new KeyValuePair<string, object?>("genre", genre));
    }

    /// <summary>
    /// Records a cloud sync operation with result.
    /// </summary>
    public static void RecordCloudSync(string provider, bool success, double durationSeconds)
    {
        ApplicationMetrics.CloudSyncOperations.Add(1,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("success", success.ToString().ToLower()));

        if (!success)
        {
            ApplicationMetrics.CloudSyncFailures.Add(1,
                new KeyValuePair<string, object?>("provider", provider));
        }

        ApplicationMetrics.CloudSyncDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("provider", provider));
    }

    /// <summary>
    /// Records an AI query with duration and token count.
    /// </summary>
    public static void RecordAiQuery(string model, bool success, double durationSeconds, long tokensUsed)
    {
        ApplicationMetrics.AiQueries.Add(1,
            new KeyValuePair<string, object?>("model", model),
            new KeyValuePair<string, object?>("success", success.ToString().ToLower()));

        if (!success)
        {
            ApplicationMetrics.AiQueryFailures.Add(1,
                new KeyValuePair<string, object?>("model", model));
        }

        ApplicationMetrics.AiQueryDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("model", model));

        ApplicationMetrics.AiTokensUsed.Add(tokensUsed,
            new KeyValuePair<string, object?>("model", model));
    }
}
