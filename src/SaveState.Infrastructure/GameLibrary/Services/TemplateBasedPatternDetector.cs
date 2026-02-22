using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.GameLibrary.Templates;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Service for detecting game memory patterns using universal templates.
/// Implements multi-pass scanning with user-triggered value change detection.
/// </summary>
public interface ITemplateBasedPatternDetector
{
    /// <summary>
    /// Detects a pattern using the specified template and strategy.
    /// </summary>
    Task<Result<DetectedPattern>> DetectPatternAsync(
        int processId,
        IMemoryPatternTemplate template,
        DetectionStrategy strategy,
        CancellationToken ct = default);

    /// <summary>
    /// Detects a pattern by template name.
    /// </summary>
    Task<Result<DetectedPattern>> DetectPatternByNameAsync(
        int processId,
        string templateName,
        DetectionStrategy strategy,
        CancellationToken ct = default);

    /// <summary>
    /// Gets user instructions for the specified template.
    /// </summary>
    string GetDetectionInstruction(string templateName);

    /// <summary>
    /// Gets all available templates.
    /// </summary>
    IReadOnlyList<IMemoryPatternTemplate> GetAvailableTemplates();

    /// <summary>
    /// Gets templates filtered by category.
    /// </summary>
    IReadOnlyList<IMemoryPatternTemplate> GetTemplatesByCategory(string category);

    /// <summary>
    /// Performs a multi-pass detection with user action prompts.
    /// </summary>
    Task<Result<DetectedPattern>> DetectWithUserActionAsync(
        int processId,
        string templateName,
        Func<string, Task> onInstruction,
        Func<Task> waitForUserAction,
        CancellationToken ct = default);
}

/// <summary>
/// Implementation of template-based pattern detection using multi-pass scanning.
/// </summary>
public sealed class TemplateBasedPatternDetector : ITemplateBasedPatternDetector
{
    private readonly IGameMemoryReader _memoryReader;
    private readonly ILogger<TemplateBasedPatternDetector> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly List<IMemoryPatternTemplate> _templates;

    public TemplateBasedPatternDetector(
        IGameMemoryReader memoryReader,
        ILogger<TemplateBasedPatternDetector> logger,
        ITimeProvider timeProvider)
    {
        _memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _templates = UniversalPatternTemplates.All.ToList();
    }

    /// <inheritdoc />
    public Task<Result<DetectedPattern>> DetectPatternAsync(
        int processId,
        IMemoryPatternTemplate template,
        DetectionStrategy strategy,
        CancellationToken ct = default)
    {
        return strategy switch
        {
            DetectionStrategy.SinglePass => DetectSinglePassAsync(processId, template, ct),
            DetectionStrategy.MultiPass => DetectMultiPassAsync(processId, template, ct),
            DetectionStrategy.UserTriggered => DetectUserTriggeredAsync(processId, template, ct),
            DetectionStrategy.SnapshotComparison => DetectSnapshotComparisonAsync(processId, template, ct),
            DetectionStrategy.StatisticalAnalysis => DetectStatisticalAsync(processId, template, ct),
            DetectionStrategy.Hybrid => DetectHybridAsync(processId, template, ct),
            _ => DetectMultiPassAsync(processId, template, ct)
        };
    }

    /// <inheritdoc />
    public async Task<Result<DetectedPattern>> DetectPatternByNameAsync(
        int processId,
        string templateName,
        DetectionStrategy strategy,
        CancellationToken ct = default)
    {
        var template = UniversalPatternTemplates.GetByName(templateName);
        if (template == null)
        {
            return Result.Failure<DetectedPattern>(
                $"Template '{templateName}' not found. Available templates: {string.Join(", ", _templates.Select(t => t.Name))}",
                ErrorType.NotFound);
        }

        return await DetectPatternAsync(processId, template, strategy, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string GetDetectionInstruction(string templateName)
    {
        var template = UniversalPatternTemplates.GetByName(templateName);
        return template?.DetectionInstruction ?? "Perform an action that changes the value you want to detect.";
    }

    /// <inheritdoc />
    public IReadOnlyList<IMemoryPatternTemplate> GetAvailableTemplates()
    {
        return _templates.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<IMemoryPatternTemplate> GetTemplatesByCategory(string category)
    {
        return UniversalPatternTemplates.GetByCategory(category);
    }

    /// <inheritdoc />
    public async Task<Result<DetectedPattern>> DetectWithUserActionAsync(
        int processId,
        string templateName,
        Func<string, Task> onInstruction,
        Func<Task> waitForUserAction,
        CancellationToken ct = default)
    {
        var template = UniversalPatternTemplates.GetByName(templateName);
        if (template == null)
        {
            return Result.Failure<DetectedPattern>(
                $"Template '{templateName}' not found",
                ErrorType.NotFound);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Step 1: Show instruction to user
            await onInstruction(template.DetectionInstruction).ConfigureAwait(false);

            // Step 2: Initial scan
            _logger.LogInformation("Starting initial scan for {TemplateName} in process {ProcessId}",
                template.Name, processId);

            var attachResult = await _memoryReader.AttachToProcessAsync(processId, ct).ConfigureAwait(false);
            if (attachResult.IsFailure)
            {
                return Result.Failure<DetectedPattern>(
                    $"Failed to attach to process: {attachResult.Error}",
                    ErrorType.Internal);
            }

            var context = new ScanContext
            {
                ValueTypes = new List<string> { "int32", "float" },
                MinConfidence = 0.3,
                FindRelatedValues = true,
                PassCount = 1
            };

            var initialScanResult = await template.ScanForMatchesAsync(_memoryReader, processId, context, ct)
                .ConfigureAwait(false);

            if (initialScanResult.IsFailure)
            {
                return Result.Failure<DetectedPattern>(
                    $"Initial scan failed: {initialScanResult.Error}",
                    initialScanResult.ErrorType);
            }

            var candidates = initialScanResult.Value;
            _logger.LogInformation("Initial scan found {Count} candidates for {TemplateName}",
                candidates.Count, template.Name);

            if (candidates.Count == 0)
            {
                return Result.Success(new DetectedPattern
                {
                    Template = template,
                    Matches = new List<PotentialMatch>(),
                    OverallConfidence = 0,
                    Strategy = DetectionStrategy.UserTriggered,
                    DetectionTime = stopwatch.Elapsed,
                    DetectedAt = _timeProvider.UtcNow
                });
            }

            // Step 3: Wait for user action
            await waitForUserAction().ConfigureAwait(false);

            // Step 4: Second scan to filter by change pattern
            await Task.Delay(500, ct).ConfigureAwait(false);

            var filteredCandidates = new List<PotentialMatch>();

            foreach (var candidate in candidates.Take(100)) // Limit to top 100 for performance
            {
                var currentValueResult = await ReadValueAtAddressAsync(
                    candidate.Address,
                    candidate.ValueType,
                    ct).ConfigureAwait(false);

                if (currentValueResult.IsSuccess)
                {
                    var currentValue = currentValueResult.Value;
                    var oldValue = candidate.Value;

                    if (template.ValidateChangePattern(oldValue, currentValue, candidate.ValueType))
                    {
                        candidate.Value = currentValue;
                        candidate.ValueHistory.Add(currentValue);
                        candidate.Confidence = template.CalculateConfidence(candidate);
                        filteredCandidates.Add(candidate);
                    }
                }
            }

            // Sort by confidence and take top results
            var finalMatches = filteredCandidates
                .OrderByDescending(m => m.Confidence)
                .Take(10)
                .ToList();

            var overallConfidence = finalMatches.Count > 0
                ? finalMatches.Average(m => m.Confidence)
                : 0;

            stopwatch.Stop();

            _logger.LogInformation(
                "Detection complete for {TemplateName}. Found {Count} matches with {Confidence:P} confidence in {ElapsedMs}ms",
                template.Name, finalMatches.Count, overallConfidence, stopwatch.ElapsedMilliseconds);

            return Result.Success(new DetectedPattern
            {
                Template = template,
                Matches = finalMatches,
                OverallConfidence = overallConfidence,
                Strategy = DetectionStrategy.UserTriggered,
                DetectionTime = stopwatch.Elapsed,
                DetectedAt = _timeProvider.UtcNow
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pattern detection for {TemplateName}", templateName);
            return Result.Failure<DetectedPattern>(
                $"Detection failed: {ex.Message}",
                ErrorType.Internal);
        }
        finally
        {
            await _memoryReader.DetachAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task<Result<DetectedPattern>> DetectSinglePassAsync(
        int processId,
        IMemoryPatternTemplate template,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var attachResult = await _memoryReader.AttachToProcessAsync(processId, ct).ConfigureAwait(false);
            if (attachResult.IsFailure)
            {
                return Result.Failure<DetectedPattern>(
                    $"Failed to attach to process: {attachResult.Error}",
                    ErrorType.Internal);
            }

            var context = new ScanContext
            {
                ValueTypes = new List<string> { "int32", "float", "double" },
                MinConfidence = 0.5,
                MaxResults = 100,
                PassCount = 1
            };

            var scanResult = await template.ScanForMatchesAsync(_memoryReader, processId, context, ct)
                .ConfigureAwait(false);

            if (scanResult.IsFailure)
            {
                return Result.Failure<DetectedPattern>(
                    $"Scan failed: {scanResult.Error}",
                    scanResult.ErrorType);
            }

            var matches = scanResult.Value
                .OrderByDescending(m => m.Confidence)
                .Take(20)
                .ToList();

            stopwatch.Stop();

            return Result.Success(new DetectedPattern
            {
                Template = template,
                Matches = matches,
                OverallConfidence = matches.Count > 0 ? matches.Average(m => m.Confidence) : 0,
                Strategy = DetectionStrategy.SinglePass,
                DetectionTime = stopwatch.Elapsed,
                DetectedAt = _timeProvider.UtcNow
            });
        }
        finally
        {
            await _memoryReader.DetachAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task<Result<DetectedPattern>> DetectMultiPassAsync(
        int processId,
        IMemoryPatternTemplate template,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var attachResult = await _memoryReader.AttachToProcessAsync(processId, ct).ConfigureAwait(false);
            if (attachResult.IsFailure)
            {
                return Result.Failure<DetectedPattern>(
                    $"Failed to attach to process: {attachResult.Error}",
                    ErrorType.Internal);
            }

            // First pass: Initial scan
            var context = new ScanContext
            {
                ValueTypes = new List<string> { "int32", "float", "double" },
                MinConfidence = 0.3,
                MaxResults = 1000,
                PassCount = 1
            };

            var firstPassResult = await template.ScanForMatchesAsync(_memoryReader, processId, context, ct)
                .ConfigureAwait(false);

            if (firstPassResult.IsFailure || firstPassResult.Value.Count == 0)
            {
                return Result.Success(new DetectedPattern
                {
                    Template = template,
                    Matches = new List<PotentialMatch>(),
                    OverallConfidence = 0,
                    Strategy = DetectionStrategy.MultiPass,
                    DetectionTime = stopwatch.Elapsed,
                    DetectedAt = _timeProvider.UtcNow
                });
            }

            // Wait between passes
            await Task.Delay(1000, ct).ConfigureAwait(false);

            // Second pass: Filter by pattern validation
            var candidates = firstPassResult.Value;
            var filteredCandidates = new List<PotentialMatch>();

            foreach (var candidate in candidates.Take(200))
            {
                var currentValueResult = await ReadValueAtAddressAsync(
                    candidate.Address,
                    candidate.ValueType,
                    ct).ConfigureAwait(false);

                if (currentValueResult.IsSuccess)
                {
                    var currentValue = currentValueResult.Value;

                    if (template.ValidateChangePattern(candidate.Value, currentValue, candidate.ValueType))
                    {
                        candidate.Value = currentValue;
                        candidate.ValueHistory.Add(currentValue);
                        candidate.Confidence = template.CalculateConfidence(candidate);
                        filteredCandidates.Add(candidate);
                    }
                }
            }

            var finalMatches = filteredCandidates
                .OrderByDescending(m => m.Confidence)
                .Take(20)
                .ToList();

            stopwatch.Stop();

            return Result.Success(new DetectedPattern
            {
                Template = template,
                Matches = finalMatches,
                OverallConfidence = finalMatches.Count > 0 ? finalMatches.Average(m => m.Confidence) : 0,
                Strategy = DetectionStrategy.MultiPass,
                DetectionTime = stopwatch.Elapsed,
                DetectedAt = _timeProvider.UtcNow
            });
        }
        finally
        {
            await _memoryReader.DetachAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task<Result<DetectedPattern>> DetectUserTriggeredAsync(
        int processId,
        IMemoryPatternTemplate template,
        CancellationToken ct)
    {
        // This requires user interaction - simplified version
        return await DetectMultiPassAsync(processId, template, ct).ConfigureAwait(false);
    }

    private async Task<Result<DetectedPattern>> DetectSnapshotComparisonAsync(
        int processId,
        IMemoryPatternTemplate template,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var attachResult = await _memoryReader.AttachToProcessAsync(processId, ct).ConfigureAwait(false);
            if (attachResult.IsFailure)
            {
                return Result.Failure<DetectedPattern>(
                    $"Failed to attach to process: {attachResult.Error}",
                    ErrorType.Internal);
            }

            // Take first snapshot
            var snapshot1 = await TakeMemorySnapshotAsync(processId, template, ct).ConfigureAwait(false);

            // Wait
            await Task.Delay(2000, ct).ConfigureAwait(false);

            // Take second snapshot
            var snapshot2 = await TakeMemorySnapshotAsync(processId, template, ct).ConfigureAwait(false);

            // Compare snapshots
            var matches = CompareSnapshots(snapshot1, snapshot2, template);

            stopwatch.Stop();

            return Result.Success(new DetectedPattern
            {
                Template = template,
                Matches = matches,
                OverallConfidence = matches.Count > 0 ? matches.Average(m => m.Confidence) : 0,
                Strategy = DetectionStrategy.SnapshotComparison,
                DetectionTime = stopwatch.Elapsed,
                DetectedAt = _timeProvider.UtcNow
            });
        }
        finally
        {
            await _memoryReader.DetachAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task<Result<DetectedPattern>> DetectStatisticalAsync(
        int processId,
        IMemoryPatternTemplate template,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var attachResult = await _memoryReader.AttachToProcessAsync(processId, ct).ConfigureAwait(false);
            if (attachResult.IsFailure)
            {
                return Result.Failure<DetectedPattern>(
                    $"Failed to attach to process: {attachResult.Error}",
                    ErrorType.Internal);
            }

            // Sample values over time
            var samples = new List<List<PotentialMatch>>();
            for (int i = 0; i < 5; i++)
            {
                var context = new ScanContext
                {
                    ValueTypes = new List<string> { "int32", "float" },
                    MinConfidence = 0.3,
                    MaxResults = 100
                };

                var scanResult = await template.ScanForMatchesAsync(_memoryReader, processId, context, ct)
                    .ConfigureAwait(false);

                if (scanResult.IsSuccess)
                {
                    samples.Add(scanResult.Value);
                }

                await Task.Delay(500, ct).ConfigureAwait(false);
            }

            // Analyze statistics
            var matches = AnalyzeStatisticalPatterns(samples, template);

            stopwatch.Stop();

            return Result.Success(new DetectedPattern
            {
                Template = template,
                Matches = matches,
                OverallConfidence = matches.Count > 0 ? matches.Average(m => m.Confidence) : 0,
                Strategy = DetectionStrategy.StatisticalAnalysis,
                DetectionTime = stopwatch.Elapsed,
                DetectedAt = _timeProvider.UtcNow
            });
        }
        finally
        {
            await _memoryReader.DetachAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task<Result<DetectedPattern>> DetectHybridAsync(
        int processId,
        IMemoryPatternTemplate template,
        CancellationToken ct)
    {
        // Combine multiple strategies for best results
        var results = new List<DetectedPattern>();

        var singlePass = await DetectSinglePassAsync(processId, template, ct).ConfigureAwait(false);
        if (singlePass.IsSuccess) results.Add(singlePass.Value);

        var multiPass = await DetectMultiPassAsync(processId, template, ct).ConfigureAwait(false);
        if (multiPass.IsSuccess) results.Add(multiPass.Value);

        var statistical = await DetectStatisticalAsync(processId, template, ct).ConfigureAwait(false);
        if (statistical.IsSuccess) results.Add(statistical.Value);

        // Combine and rank results
        var combinedMatches = results
            .SelectMany(r => r.Matches)
            .GroupBy(m => m.Address)
            .Select(g =>
            {
                var best = g.OrderByDescending(m => m.Confidence).First();
                best.Confidence = Math.Min(g.Average(m => m.Confidence) + 0.1, 1.0);
                return best;
            })
            .OrderByDescending(m => m.Confidence)
            .Take(20)
            .ToList();

        return Result.Success(new DetectedPattern
        {
            Template = template,
            Matches = combinedMatches,
            OverallConfidence = combinedMatches.Count > 0 ? combinedMatches.Average(m => m.Confidence) : 0,
            Strategy = DetectionStrategy.Hybrid,
            DetectionTime = results.Aggregate(TimeSpan.Zero, (acc, r) => acc + r.DetectionTime),
            DetectedAt = _timeProvider.UtcNow
        });
    }

    private async Task<Result<object>> ReadValueAtAddressAsync(IntPtr address, string valueType, CancellationToken ct)
    {
        try
        {
            var bytesResult = await _memoryReader.ReadMemoryBytesAsync(address, GetValueSize(valueType), ct)
                .ConfigureAwait(false);

            if (bytesResult.IsFailure)
            {
                return Result.Failure<object>(bytesResult.Error!, bytesResult.ErrorType);
            }

            var bytes = bytesResult.Value;
            var value = ConvertBytesToValue(bytes, valueType);
            return Result.Success(value);
        }
        catch (Exception ex)
        {
            return Result.Failure<object>($"Failed to read value: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<List<PotentialMatch>> TakeMemorySnapshotAsync(
        int processId,
        IMemoryPatternTemplate template,
        CancellationToken ct)
    {
        var context = new ScanContext
        {
            ValueTypes = new List<string> { "int32", "float" },
            MinConfidence = 0.3,
            MaxResults = 500
        };

        var result = await template.ScanForMatchesAsync(_memoryReader, processId, context, ct)
            .ConfigureAwait(false);

        return result.IsSuccess ? result.Value : new List<PotentialMatch>();
    }

    private List<PotentialMatch> CompareSnapshots(
        List<PotentialMatch> snapshot1,
        List<PotentialMatch> snapshot2,
        IMemoryPatternTemplate template)
    {
        var matches = new List<PotentialMatch>();

        var snapshot2Dict = snapshot2.ToDictionary(s => s.Address, s => s);

        foreach (var s1 in snapshot1)
        {
            if (snapshot2Dict.TryGetValue(s1.Address, out var s2))
            {
                if (template.ValidateChangePattern(s1.Value, s2.Value, s1.ValueType))
                {
                    s2.ValueHistory = new List<object> { s1.Value, s2.Value };
                    s2.Confidence = template.CalculateConfidence(s2);
                    matches.Add(s2);
                }
            }
        }

        return matches.OrderByDescending(m => m.Confidence).ToList();
    }

    private List<PotentialMatch> AnalyzeStatisticalPatterns(
        List<List<PotentialMatch>> samples,
        IMemoryPatternTemplate template)
    {
        // Find addresses that appear consistently across samples
        var addressFrequency = new Dictionary<IntPtr, List<PotentialMatch>>();

        foreach (var sample in samples)
        {
            foreach (var match in sample)
            {
                if (!addressFrequency.ContainsKey(match.Address))
                {
                    addressFrequency[match.Address] = new List<PotentialMatch>();
                }
                addressFrequency[match.Address].Add(match);
            }
        }

        // Addresses that appear in most samples are more reliable
        var consistentAddresses = addressFrequency
            .Where(kvp => kvp.Value.Count >= samples.Count * 0.6)
            .Select(kvp =>
            {
                var best = kvp.Value.OrderByDescending(m => m.Confidence).First();
                best.Confidence = Math.Min(best.Confidence + 0.1, 1.0);
                return best;
            })
            .OrderByDescending(m => m.Confidence)
            .Take(20)
            .ToList();

        return consistentAddresses;
    }

    private static int GetValueSize(string valueType)
    {
        return valueType.ToLowerInvariant() switch
        {
            "int8" or "byte" => 1,
            "int16" or "short" => 2,
            "int32" or "int" => 4,
            "int64" or "long" => 8,
            "float" => 4,
            "double" => 8,
            _ => 4
        };
    }

    private static object ConvertBytesToValue(byte[] bytes, string valueType)
    {
        return valueType.ToLowerInvariant() switch
        {
            "int32" or "int" => BitConverter.ToInt32(bytes, 0),
            "int64" or "long" => BitConverter.ToInt64(bytes, 0),
            "float" => BitConverter.ToSingle(bytes, 0),
            "double" => BitConverter.ToDouble(bytes, 0),
            _ => BitConverter.ToInt32(bytes, 0)
        };
    }
}
