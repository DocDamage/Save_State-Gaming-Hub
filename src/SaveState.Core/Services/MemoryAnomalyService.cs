using SaveState.Core.Interfaces;
using SaveState.Core.Models;
using Serilog;

namespace SaveState.Core.Services;

/// <summary>
/// Memory-Based Anomaly Detection service using statistical analysis
/// For production, integrate ML.NET for more sophisticated detection
/// </summary>
public class MemoryAnomalyService : IMemoryAnomalyService
{
    private readonly ILogger _logger = Log.ForContext<MemoryAnomalyService>();
    private readonly List<MemorySnapshot> _snapshots = new();
    private readonly object _lock = new();
    
    // Configuration
    private const int MaxHistorySize = 1000;
    private const double AnomalyThreshold = 0.7;
    private const double RapidChangeThreshold = 0.5; // Max change rate per ms
    
    // Baseline statistics (learned during training)
    private Dictionary<long, Statistics> _addressBaselines = new();
    private Statistics? _writeCountBaseline;
    private bool _isTrained = false;

    public bool IsCheatDetected { get; private set; }
    public double CurrentAnomalyScore { get; private set; }
    public bool IsModelTrained => _isTrained;

    public Task RecordSnapshotAsync(MemorySnapshot snapshot)
    {
        lock (_lock)
        {
            _snapshots.Add(snapshot);
            
            // Trim history if needed
            if (_snapshots.Count > MaxHistorySize)
            {
                _snapshots.RemoveAt(0);
            }
        }

        return Task.CompletedTask;
    }

    public Task<AnomalyResult> AnalyzeAsync()
    {
        lock (_lock)
        {
            if (_snapshots.Count < 2)
            {
                return Task.FromResult(AnomalyResult.None);
            }

            var latest = _snapshots[^1];
            var previous = _snapshots[^2];
            var features = new Dictionary<string, double>();
            var suspiciousAddresses = new List<long>();
            double totalScore = 0;
            int featureCount = 0;

            // Feature 1: Rapid value changes
            foreach (var addr in latest.WatchedAddresses.Keys)
            {
                if (previous.WatchedAddresses.TryGetValue(addr, out int prevValue))
                {
                    var currentValue = latest.WatchedAddresses[addr];
                    var delta = Math.Abs(currentValue - prevValue);
                    var deltaMs = latest.DeltaMs > 0 ? latest.DeltaMs : 1;
                    var changeRate = delta / deltaMs;

                    if (changeRate > RapidChangeThreshold)
                    {
                        var anomalyContribution = Math.Min(1.0, changeRate / (RapidChangeThreshold * 10));
                        features[$"RapidChange_{addr:X}"] = anomalyContribution;
                        totalScore += anomalyContribution;
                        featureCount++;
                        suspiciousAddresses.Add(addr);
                    }

                    // Check against baseline if trained
                    if (_isTrained && _addressBaselines.TryGetValue(addr, out var baseline))
                    {
                        var zScore = Math.Abs((currentValue - baseline.Mean) / Math.Max(baseline.StdDev, 1));
                        if (zScore > 3) // 3 sigma rule
                        {
                            features[$"StatisticalOutlier_{addr:X}"] = Math.Min(1.0, zScore / 10);
                            totalScore += features[$"StatisticalOutlier_{addr:X}"];
                            featureCount++;
                            if (!suspiciousAddresses.Contains(addr))
                                suspiciousAddresses.Add(addr);
                        }
                    }
                }
            }

            // Feature 2: Abnormal write count
            if (_isTrained && _writeCountBaseline != null)
            {
                var zScore = Math.Abs((latest.WriteCount - _writeCountBaseline.Mean) / Math.Max(_writeCountBaseline.StdDev, 1));
                if (zScore > 2)
                {
                    features["AbnormalWriteCount"] = Math.Min(1.0, zScore / 5);
                    totalScore += features["AbnormalWriteCount"];
                    featureCount++;
                }
            }
            else if (latest.WriteCount > 100) // Heuristic without training
            {
                features["HighWriteCount"] = Math.Min(1.0, latest.WriteCount / 500.0);
                totalScore += features["HighWriteCount"];
                featureCount++;
            }

            // Feature 3: Impossible value detection (e.g., health going from 0 to max instantly)
            foreach (var addr in latest.WatchedAddresses.Keys)
            {
                if (previous.WatchedAddresses.TryGetValue(addr, out int prevValue))
                {
                    var currentValue = latest.WatchedAddresses[addr];
                    // Detect resurrection patterns (0 -> high value)
                    if (prevValue <= 0 && currentValue > 1000 && latest.DeltaMs < 100)
                    {
                        features[$"ImpossibleValue_{addr:X}"] = 0.9;
                        totalScore += 0.9;
                        featureCount++;
                        if (!suspiciousAddresses.Contains(addr))
                            suspiciousAddresses.Add(addr);
                    }
                }
            }

            // Calculate final score
            CurrentAnomalyScore = featureCount > 0 ? totalScore / featureCount : 0;
            IsCheatDetected = CurrentAnomalyScore > AnomalyThreshold;

            if (IsCheatDetected)
            {
                var dominantFeature = features.OrderByDescending(f => f.Value).FirstOrDefault();
                var anomalyType = dominantFeature.Key?.Split('_')[0] ?? AnomalyTypes.StatisticalOutlier;

                _logger.Warning("Anomaly detected! Score: {Score:P1}, Type: {Type}", 
                    CurrentAnomalyScore, anomalyType);

                return Task.FromResult(new AnomalyResult
                {
                    IsAnomaly = true,
                    ConfidenceScore = CurrentAnomalyScore,
                    AnomalyType = anomalyType,
                    Description = $"Detected {anomalyType} with {CurrentAnomalyScore:P0} confidence",
                    FeatureContributions = features,
                    SuspiciousAddresses = suspiciousAddresses
                });
            }

            return Task.FromResult(AnomalyResult.None);
        }
    }

    public Task TrainModelAsync(IEnumerable<MemorySnapshot> normalBehavior)
    {
        var snapshots = normalBehavior.ToList();
        if (snapshots.Count < 10)
        {
            _logger.Warning("Insufficient training data ({Count} snapshots, need at least 10)", snapshots.Count);
            return Task.CompletedTask;
        }

        _logger.Information("Training MBAD model with {Count} snapshots", snapshots.Count);

        // Calculate baseline statistics for each watched address
        var addressValues = new Dictionary<long, List<int>>();
        var writeCounts = new List<double>();

        foreach (var snapshot in snapshots)
        {
            foreach (var (addr, value) in snapshot.WatchedAddresses)
            {
                if (!addressValues.ContainsKey(addr))
                    addressValues[addr] = new List<int>();
                addressValues[addr].Add(value);
            }
            writeCounts.Add(snapshot.WriteCount);
        }

        // Calculate statistics
        _addressBaselines.Clear();
        foreach (var (addr, values) in addressValues)
        {
            _addressBaselines[addr] = CalculateStatistics(values.Select(v => (double)v));
        }

        if (writeCounts.Count > 0)
        {
            _writeCountBaseline = CalculateStatistics(writeCounts);
        }

        _isTrained = true;
        _logger.Information("MBAD model trained with {AddressCount} address baselines", _addressBaselines.Count);

        return Task.CompletedTask;
    }

    public async Task LoadModelAsync(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            _logger.Warning("Model file not found: {Path}", modelPath);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(modelPath);
            var model = System.Text.Json.JsonSerializer.Deserialize<SerializableModel>(json);
            
            if (model != null)
            {
                _addressBaselines = model.AddressBaselines
                    .ToDictionary(
                        kvp => long.Parse(kvp.Key),
                        kvp => new Statistics { Mean = kvp.Value.Mean, StdDev = kvp.Value.StdDev });
                
                if (model.WriteCountBaseline != null)
                {
                    _writeCountBaseline = new Statistics 
                    { 
                        Mean = model.WriteCountBaseline.Mean, 
                        StdDev = model.WriteCountBaseline.StdDev 
                    };
                }
                
                _isTrained = true;
                _logger.Information("Loaded MBAD model from {Path} with {Count} baselines", 
                    modelPath, _addressBaselines.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load model from {Path}", modelPath);
        }
    }

    public async Task SaveModelAsync(string modelPath)
    {
        if (!_isTrained)
        {
            _logger.Warning("Cannot save model: not trained yet");
            return;
        }

        try
        {
            var model = new SerializableModel
            {
                AddressBaselines = _addressBaselines.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => new SerializableStatistics { Mean = kvp.Value.Mean, StdDev = kvp.Value.StdDev }),
                WriteCountBaseline = _writeCountBaseline != null 
                    ? new SerializableStatistics { Mean = _writeCountBaseline.Mean, StdDev = _writeCountBaseline.StdDev }
                    : null,
                TrainedAt = DateTime.UtcNow
            };

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var json = System.Text.Json.JsonSerializer.Serialize(model, options);
            
            var directory = Path.GetDirectoryName(modelPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllTextAsync(modelPath, json);
            _logger.Information("Saved MBAD model to {Path}", modelPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save model to {Path}", modelPath);
        }
    }

    public void ClearHistory()
    {
        lock (_lock)
        {
            _snapshots.Clear();
            IsCheatDetected = false;
            CurrentAnomalyScore = 0;
        }
    }

    private static Statistics CalculateStatistics(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count == 0)
            return new Statistics { Mean = 0, StdDev = 0 };

        var mean = list.Average();
        var variance = list.Sum(v => Math.Pow(v - mean, 2)) / list.Count;
        var stdDev = Math.Sqrt(variance);

        return new Statistics { Mean = mean, StdDev = stdDev };
    }

    private record Statistics
    {
        public double Mean { get; init; }
        public double StdDev { get; init; }
    }

    // Serialization models for JSON persistence
    private class SerializableModel
    {
        public Dictionary<string, SerializableStatistics> AddressBaselines { get; set; } = new();
        public SerializableStatistics? WriteCountBaseline { get; set; }
        public DateTime TrainedAt { get; set; }
    }

    private class SerializableStatistics
    {
        public double Mean { get; set; }
        public double StdDev { get; set; }
    }
}

