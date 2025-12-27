using SaveState.Core.Models;

namespace SaveState.Core.Interfaces;

/// <summary>
/// Memory-Based Anomaly Detection service for detecting cheating patterns
/// </summary>
public interface IMemoryAnomalyService
{
    /// <summary>
    /// Record a memory snapshot for analysis
    /// </summary>
    Task RecordSnapshotAsync(MemorySnapshot snapshot);

    /// <summary>
    /// Analyze recent snapshots for anomalies
    /// </summary>
    Task<AnomalyResult> AnalyzeAsync();

    /// <summary>
    /// Train the anomaly detection model with normal behavior data
    /// </summary>
    Task TrainModelAsync(IEnumerable<MemorySnapshot> normalBehavior);

    /// <summary>
    /// Load a pre-trained model from file
    /// </summary>
    Task LoadModelAsync(string modelPath);

    /// <summary>
    /// Save the current model to file
    /// </summary>
    Task SaveModelAsync(string modelPath);

    /// <summary>
    /// Whether an active cheat is currently detected
    /// </summary>
    bool IsCheatDetected { get; }

    /// <summary>
    /// Current anomaly score (0-1, higher = more anomalous)
    /// </summary>
    double CurrentAnomalyScore { get; }

    /// <summary>
    /// Clear all recorded snapshots
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Whether the model is trained and ready
    /// </summary>
    bool IsModelTrained { get; }
}
