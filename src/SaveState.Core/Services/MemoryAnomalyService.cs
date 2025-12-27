using SaveState.Core.Interfaces;
using SaveState.Core.Models;
using Serilog;

namespace SaveState.Core.Services;

/// <summary>
/// Memory-Based Anomaly Detection for cheat monitoring
/// Detects when game resets our values, monitors cheat effectiveness,
/// and alerts when anti-cheat might be interfering
/// </summary>
public class MemoryAnomalyService : IMemoryAnomalyService
{
    private readonly ILogger _logger = Log.ForContext<MemoryAnomalyService>();
    private readonly List<MemorySnapshot> _snapshots = new();
    private readonly List<CheatAlert> _alerts = new();
    private readonly Dictionary<long, int> _expectedValues = new();
    private readonly object _lock = new();
    
    // Configuration
    private const int MaxHistorySize = 1000;
    private const int MaxAlertHistory = 100;
    
    // Baseline statistics
    private Dictionary<long, Statistics> _addressBaselines = new();
    private Statistics? _writeCountBaseline;
    private bool _isTrained = false;

    public bool IsCheatDetected { get; private set; }
    public double CurrentAnomalyScore { get; private set; }
    public bool IsModelTrained => _isTrained;
    public IReadOnlyList<CheatAlert> RecentAlerts => _alerts.AsReadOnly();
    
    /// <summary>
    /// Event fired when game appears to reset our cheat values
    /// </summary>
    public event Action<CheatAlert>? OnValueReset;
    
    /// <summary>
    /// Event fired when cheats appear to be working well
    /// </summary>
    public event Action<string>? OnCheatConfirmed;

    /// <summary>
    /// Set expected value for an address (what we wrote)
    /// Used to detect when game resets our values
    /// </summary>
    public void SetExpectedValue(long address, int value)
    {
        lock (_lock)
        {
            _expectedValues[address] = value;
            _logger.Debug("Tracking address {Address:X} with expected value {Value}", address, value);
        }
    }

    /// <summary>
    /// Clear expected value tracking for an address
    /// </summary>
    public void ClearExpectedValue(long address)
    {
        lock (_lock)
        {
            _expectedValues.Remove(address);
        }
    }

    public Task RecordSnapshotAsync(MemorySnapshot snapshot)
    {
        lock (_lock)
        {
            _snapshots.Add(snapshot);
            
            if (_snapshots.Count > MaxHistorySize)
            {
                _snapshots.RemoveAt(0);
            }
        }

        return Task.CompletedTask;
    }

    public async Task<AnomalyResult> AnalyzeAsync()
    {
        var result = AnalyzeCheatStatus();
        
        if (result.IsAnomaly)
        {
            CurrentAnomalyScore = result.ConfidenceScore;
            
            // This means something is interfering with our cheats
            var alert = new CheatAlert
            {
                Type = result.AnomalyType,
                Message = result.Description,
                AffectedAddresses = result.SuspiciousAddresses,
                DetectedAt = DateTime.UtcNow
            };
            
            lock (_lock)
            {
                _alerts.Add(alert);
                if (_alerts.Count > MaxAlertHistory)
                    _alerts.RemoveAt(0);
            }
            
            OnValueReset?.Invoke(alert);
            _logger.Warning("Cheat interference detected: {Type} - {Description}", 
                result.AnomalyType, result.Description);
        }
        else if (_snapshots.Count > 0 && _expectedValues.Count > 0)
        {
            // Cheats appear to be holding
            var holdingCount = CountHoldingValues();
            if (holdingCount == _expectedValues.Count)
            {
                OnCheatConfirmed?.Invoke($"All {holdingCount} cheat values holding steady");
            }
        }
        
        return await Task.FromResult(result);
    }

    /// <summary>
    /// Analyze if our cheats are being interfered with
    /// </summary>
    private AnomalyResult AnalyzeCheatStatus()
    {
        lock (_lock)
        {
            if (_snapshots.Count < 1)
            {
                return AnomalyResult.None;
            }

            var latest = _snapshots[^1];
            var features = new Dictionary<string, double>();
            var affectedAddresses = new List<long>();
            
            // Check 1: Are our expected values being reset?
            foreach (var (addr, expectedValue) in _expectedValues)
            {
                if (latest.WatchedAddresses.TryGetValue(addr, out int actualValue))
                {
                    if (actualValue != expectedValue)
                    {
                        var deviation = Math.Abs(actualValue - expectedValue);
                        features[$"ValueReset_{addr:X}"] = 1.0;
                        affectedAddresses.Add(addr);
                        
                        _logger.Warning("Value reset detected at {Address:X}: expected {Expected}, got {Actual}",
                            addr, expectedValue, actualValue);
                    }
                }
            }

            // Check 2: Rapid successive resets (anti-cheat actively fighting)
            if (_snapshots.Count >= 3 && affectedAddresses.Count > 0)
            {
                var recentSnapshots = _snapshots.TakeLast(3).ToList();
                int resetCount = 0;
                
                foreach (var addr in affectedAddresses)
                {
                    if (_expectedValues.TryGetValue(addr, out int expected))
                    {
                        var values = recentSnapshots
                            .Where(s => s.WatchedAddresses.ContainsKey(addr))
                            .Select(s => s.WatchedAddresses[addr])
                            .ToList();
                        
                        // Count how many times value != expected
                        resetCount += values.Count(v => v != expected);
                    }
                }
                
                if (resetCount >= affectedAddresses.Count * 2)
                {
                    features["ActiveAntiCheat"] = 0.9;
                }
            }

            // Check 3: Values decreasing when they should be frozen
            if (_snapshots.Count >= 2)
            {
                var previous = _snapshots[^2];
                foreach (var (addr, expectedValue) in _expectedValues)
                {
                    if (latest.WatchedAddresses.TryGetValue(addr, out int currentVal) &&
                        previous.WatchedAddresses.TryGetValue(addr, out int prevVal))
                    {
                        // If we expected a freeze but value keeps changing
                        if (currentVal != expectedValue && prevVal != expectedValue &&
                            currentVal != prevVal)
                        {
                            features[$"FreezeFailure_{addr:X}"] = 0.7;
                            if (!affectedAddresses.Contains(addr))
                                affectedAddresses.Add(addr);
                        }
                    }
                }
            }

            // Check 4: Sudden memory access errors (game protecting memory)
            if (latest.ReadCount == 0 && _snapshots.Count > 5)
            {
                var avgReads = _snapshots.TakeLast(5).Average(s => s.ReadCount);
                if (avgReads > 10)
                {
                    features["MemoryProtection"] = 0.8;
                }
            }

            if (features.Count > 0)
            {
                var topFeature = features.OrderByDescending(f => f.Value).First();
                string description = topFeature.Key switch
                {
                    var k when k.StartsWith("ValueReset") => 
                        $"Game reset {affectedAddresses.Count} cheat value(s) - try freezing or re-applying",
                    var k when k == "ActiveAntiCheat" => 
                        "Anti-cheat actively resetting values - consider pointer-based cheats",
                    var k when k.StartsWith("FreezeFailure") => 
                        "Freeze not working - value keeps changing despite writes", 
                    var k when k == "MemoryProtection" =>
                        "Memory access blocked - game may have protected memory regions",
                    _ => $"Cheat interference: {topFeature.Key}"
                };

                return new AnomalyResult
                {
                    IsAnomaly = true,
                    ConfidenceScore = features.Values.Average(),
                    AnomalyType = topFeature.Key.Split('_')[0],
                    Description = description,
                    FeatureContributions = features,
                    SuspiciousAddresses = affectedAddresses,
                    DetectedAt = DateTime.UtcNow
                };
            }

            return AnomalyResult.None;
        }
    }

    /// <summary>
    /// Count how many expected values are currently holding
    /// </summary>
    public int CountHoldingValues()
    {
        lock (_lock)
        {
            if (_snapshots.Count == 0) return 0;
            
            var latest = _snapshots[^1];
            int holding = 0;
            
            foreach (var (addr, expected) in _expectedValues)
            {
                if (latest.WatchedAddresses.TryGetValue(addr, out int actual))
                {
                    if (actual == expected)
                        holding++;
                }
            }
            
            return holding;
        }
    }

    /// <summary>
    /// Get status summary of all tracked cheats
    /// </summary>
    public CheatStatusSummary GetStatusSummary()
    {
        lock (_lock)
        {
            var latest = _snapshots.LastOrDefault();
            var statuses = new Dictionary<long, CheatValueStatus>();
            
            foreach (var (addr, expected) in _expectedValues)
            {
                var currentValue = latest?.WatchedAddresses.GetValueOrDefault(addr) ?? 0;
                var status = new CheatValueStatus
                {
                    Address = addr,
                    ExpectedValue = expected,
                    CurrentValue = currentValue,
                    IsHolding = currentValue == expected
                };
                
                statuses[addr] = status;
            }
            
            return new CheatStatusSummary
            {
                TrackedCount = _expectedValues.Count,
                HoldingCount = statuses.Values.Count(s => s.IsHolding),
                FailingCount = statuses.Values.Count(s => !s.IsHolding),
                Statuses = statuses,
                LastCheck = DateTime.UtcNow
            };
        }
    }

    public Task TrainModelAsync(IEnumerable<MemorySnapshot> normalBehavior)
    {
        var snapshots = normalBehavior.ToList();
        if (snapshots.Count < 10)
        {
            _logger.Warning("Insufficient training data ({Count} snapshots)", snapshots.Count);
            return Task.CompletedTask;
        }

        _logger.Information("Training baseline with {Count} snapshots", snapshots.Count);

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
        _logger.Information("Baseline trained with {AddressCount} addresses", _addressBaselines.Count);

        return Task.CompletedTask;
    }

    public async Task LoadModelAsync(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            _logger.Debug("No saved model at: {Path}", modelPath);
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
                _logger.Information("Loaded model from {Path}", modelPath);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load model");
        }
    }

    public async Task SaveModelAsync(string modelPath)
    {
        if (!_isTrained)
        {
            _logger.Warning("Cannot save: not trained");
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

            var directory = Path.GetDirectoryName(modelPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            var json = System.Text.Json.JsonSerializer.Serialize(model, 
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(modelPath, json);
            
            _logger.Information("Saved model to {Path}", modelPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save model");
        }
    }

    public void ClearHistory()
    {
        lock (_lock)
        {
            _snapshots.Clear();
            _alerts.Clear();
            _expectedValues.Clear();
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

        return new Statistics { Mean = mean, StdDev = Math.Sqrt(variance) };
    }

    private record Statistics { public double Mean { get; init; } public double StdDev { get; init; } }
    private class SerializableModel
    {
        public Dictionary<string, SerializableStatistics> AddressBaselines { get; set; } = new();
        public SerializableStatistics? WriteCountBaseline { get; set; }
        public DateTime TrainedAt { get; set; }
    }
    private class SerializableStatistics { public double Mean { get; set; } public double StdDev { get; set; } }
}

/// <summary>
/// Alert when game interferes with cheats
/// </summary>
public class CheatAlert
{
    public string Type { get; init; } = "";
    public string Message { get; init; } = "";
    public List<long> AffectedAddresses { get; init; } = new();
    public DateTime DetectedAt { get; init; }
}

/// <summary>
/// Status of a single cheat value
/// </summary>
public class CheatValueStatus
{
    public long Address { get; init; }
    public int ExpectedValue { get; init; }
    public int CurrentValue { get; init; }
    public bool IsHolding { get; init; }
}

/// <summary>
/// Summary of all tracked cheat statuses
/// </summary>
public class CheatStatusSummary
{
    public int TrackedCount { get; init; }
    public int HoldingCount { get; init; }
    public int FailingCount { get; init; }
    public Dictionary<long, CheatValueStatus> Statuses { get; init; } = new();
    public DateTime LastCheck { get; init; }
}
