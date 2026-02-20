using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.GameLibrary.Heuristics;
using SaveState.Application.Common;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// AI-powered memory pattern auto-discovery engine.
/// Automatically discovers game values without prior knowledge or signatures.
/// </summary>
public sealed class AutoDiscoveryEngine : IAutoDiscoveryEngine, IDisposable
{
    private readonly ILogger<AutoDiscoveryEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly List<IValueHeuristic> _heuristics;
    private readonly Dictionary<Guid, DiscoverySessionContext> _activeSessions = new();
    private readonly Dictionary<string, HeuristicFeedbackData> _feedbackHistory = new();
    private readonly object _sessionLock = new();

    // Windows API for memory reading
    [Flags]
    private enum ProcessAccessRights : uint
    {
        ProcessVmRead = 0x0010,
        ProcessVmWrite = 0x0020,
        ProcessVmOperation = 0x0008,
        ProcessQueryInformation = 0x0400
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(ProcessAccessRights dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out uint lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_INFO
    {
        public ushort wProcessorArchitecture;
        public ushort wReserved;
        public uint dwPageSize;
        public IntPtr lpMinimumApplicationAddress;
        public IntPtr lpMaximumApplicationAddress;
        public IntPtr dwActiveProcessorMask;
        public uint dwNumberOfProcessors;
        public uint dwProcessorType;
        public uint dwAllocationGranularity;
        public ushort wProcessorLevel;
        public ushort wProcessorRevision;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoDiscoveryEngine"/> class.
    /// </summary>
    public AutoDiscoveryEngine(ILogger<AutoDiscoveryEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        // Initialize all heuristics (7 original + 17 new = 24 total)
        _heuristics = new List<IValueHeuristic>
        {
            // Original 7 heuristics
            new HealthHeuristic(),
            new CurrencyHeuristic(),
            new PositionHeuristic(),
            new AmmoHeuristic(),
            new ExperienceHeuristic(),
            new ScoreHeuristic(),
            new TimerHeuristic(),

            // Movement & Physics (4)
            new SpeedHeuristic(),
            new VelocityHeuristic(),
            new JumpHeightHeuristic(),
            new GravityHeuristic(),

            // Combat Mechanics (4)
            new CooldownHeuristic(),
            new DamageHeuristic(),
            new CriticalChanceHeuristic(),
            new ArmorRatingHeuristic(),

            // RPG Progression (3)
            new SkillPointsHeuristic(),
            new ReputationHeuristic(),
            new CarryWeightHeuristic(),

            // Resource Management (3)
            new ManaHeuristic(),
            new DurabilityHeuristic(),
            new ResourceCountHeuristic(),

            // Game State (3)
            new DifficultyHeuristic(),
            new GameTimeHeuristic(),
            new CompletionHeuristic()
        };

        _logger.LogInformation("AutoDiscoveryEngine initialized with {Count} heuristics", _heuristics.Count);
    }

    /// <inheritdoc />
    public Task<Result<DiscoverySession>> StartDiscoverySessionAsync(int processId, DiscoveryOptions options, CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid();
        
        using (_logger.BeginCorrelationScope(sessionId.ToString("N")))
        using (_logger.BeginSessionScope(sessionId))
        {
            _logger.LogInformation(
                "Starting discovery session {SessionId} for process {ProcessId}. ScanRange: {StartAddress:X}-{EndAddress:X}",
                sessionId,
                processId,
                options.ScanStartAddress,
                options.ScanStartAddress + options.ScanSize);
                
            try
            {
                // Validate process exists
                Process? process = null;
                try
                {
                    process = Process.GetProcessById(processId);
                }
                catch (ArgumentException)
                {
                    _logger.LogError("Process {ProcessId} not found", processId);
                    return Task.FromResult(Result.Failure<DiscoverySession>($"Process {processId} not found", ErrorType.NotFound));
                }

                // Open process handle
                var processHandle = OpenProcess(ProcessAccessRights.ProcessVmRead, false, processId);
                if (processHandle == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError(
                        "Failed to start discovery session {SessionId}: Win32 error {Error}", 
                        sessionId, 
                        error);
                    return Task.FromResult(Result.Failure<DiscoverySession>(
                        $"Failed to open process for memory reading (Win32 error: {error})", ErrorType.External));
                }

                // Create session (SessionId is auto-generated)
                var session = new DiscoverySession
                {
                    ProcessId = processId,
                    Options = options,
                    IsActive = true,
                    CurrentPass = 0
                };

                var context = new DiscoverySessionContext
                {
                    Session = session,
                    ProcessHandle = processHandle,
                    Process = process
                };

                lock (_sessionLock)
                {
                    _activeSessions[session.SessionId] = context;
                }

                _logger.LogInformation(
                    "Discovery session {SessionId} initialized. Scan range: {StartAddress:X} - {EndAddress:X}",
                    sessionId,
                    options.ScanStartAddress,
                    options.ScanStartAddress + options.ScanSize);
                    
                return Task.FromResult(Result.Success(session));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start discovery session {SessionId}", sessionId);
                return Task.FromResult(Result.Failure<DiscoverySession>(
                    $"Failed to start discovery session: {ex.Message}", ErrorType.Internal));
            }
        }
    }

    /// <inheritdoc />
    public async Task<Result<DiscoveryResult>> AnalyzeChangeAsync(DiscoverySession session, PlayerAction action, CancellationToken ct = default)
    {
        using (_logger.BeginCorrelationScope())
        using (_logger.BeginDiscoveryAnalysisScope(action.ToString(), session.SessionId))
        {
            var beforeCount = session.Candidates.Count;
            
            _logger.LogInformation(
                "Analyzing player action {Action} in session {SessionId}. Candidates before: {CandidateCount}",
                action,
                session.SessionId,
                beforeCount);
                
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                if (session == null)
                    return Result.Failure<DiscoveryResult>("Session cannot be null", ErrorType.Validation);

                if (!session.IsActive)
                    return Result.Failure<DiscoveryResult>("Session is not active", ErrorType.Validation);

                DiscoverySessionContext? context;
                lock (_sessionLock)
                {
                    if (!_activeSessions.TryGetValue(session.SessionId, out context))
                        return Result.Failure<DiscoveryResult>("Session not found", ErrorType.NotFound);
                }

                // Record the action
                var actionRecord = new PlayerActionRecord
                {
                    Timestamp = _timeProvider.UtcNow,
                    Action = action
                };
                session.ActionHistory.Add(actionRecord);

                // Perform a scan pass
                await PerformDiscoveryPassAsync(session, context, action, ct).ConfigureAwait(false);

                // Apply heuristics and rank candidates
                var rankedCandidates = ApplyHeuristicsAndRank(session);

                // Update session with top candidates
                session.Candidates.Clear();
                session.Candidates.AddRange(rankedCandidates.Take(session.Options.MaxCandidates));

                // Build result
                var afterCount = session.Candidates.Count;
                var topConfidence = rankedCandidates.FirstOrDefault()?.ConfidenceScore ?? 0;
                
                var result = new DiscoveryResult
                {
                    SessionId = session.SessionId,
                    AnalyzedAction = action,
                    RemainingCandidates = afterCount,
                    EliminatedCandidates = Math.Max(0, beforeCount - afterCount),
                    TopValues = rankedCandidates.Take(10).ToList(),
                    ConfidenceImproved = session.Candidates.Any(c => c.ConfidenceScore > 0.5)
                };

                stopwatch.Stop();
                
                _logger.LogInformation(
                    "Action analysis complete. Filtered from {BeforeCount} to {AfterCount} candidates in {ElapsedMs}ms. " +
                    "Top confidence: {TopConfidence:P}",
                    beforeCount,
                    afterCount,
                    stopwatch.ElapsedMilliseconds,
                    topConfidence);
                    
                return Result.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Action analysis failed for {Action} after {ElapsedMs}ms", action, stopwatch.ElapsedMilliseconds);
                return Result.Failure<DiscoveryResult>($"Failed to analyze change: {ex.Message}", ErrorType.Internal);
            }
        }
    }

    /// <inheritdoc />
    public Task<Result<List<DiscoveredValue>>> GetRankedResultsAsync(DiscoverySession session, CancellationToken ct = default)
    {
        try
        {
            if (session == null)
                return Task.FromResult(Result.Failure<List<DiscoveredValue>>("Session cannot be null", ErrorType.Validation));

            if (!session.IsActive)
                return Task.FromResult(Result.Failure<List<DiscoveredValue>>("Session is not active", ErrorType.Validation));

            _logger.LogDebug(
                "Getting ranked results for session {SessionId}. Threshold: {Threshold}, MaxResults: {MaxResults}",
                session.SessionId,
                session.Options.MinConfidenceThreshold,
                session.Options.MaxResults);

            // Return ranked results filtered by confidence threshold
            var results = session.Candidates
                .Where(c => c.ConfidenceScore >= session.Options.MinConfidenceThreshold)
                .OrderByDescending(c => c.ConfidenceScore)
                .Take(session.Options.MaxResults)
                .ToList();

            _logger.LogInformation(
                "Returning {Count} ranked results for session {SessionId}",
                results.Count,
                session.SessionId);

            return Task.FromResult(Result.Success(results));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ranked results for session {SessionId}", session?.SessionId);
            return Task.FromResult(Result.Failure<List<DiscoveredValue>>(
                $"Failed to get ranked results: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> StopDiscoverySessionAsync(DiscoverySession session, CancellationToken ct = default)
    {
        try
        {
            if (session == null)
                return Task.FromResult(Result.Failure("Session cannot be null", ErrorType.Validation));

            _logger.LogInformation("Stopping discovery session {SessionId}", session.SessionId);

            lock (_sessionLock)
            {
                if (!_activeSessions.TryGetValue(session.SessionId, out var context))
                    return Task.FromResult(Result.Failure("Session not found", ErrorType.NotFound));

                // Close process handle
                if (context.ProcessHandle != IntPtr.Zero)
                {
                    CloseHandle(context.ProcessHandle);
                    context.ProcessHandle = IntPtr.Zero;
                }

                context.Process?.Dispose();
                _activeSessions.Remove(session.SessionId);
            }

            session.IsActive = false;

            _logger.LogInformation("Discovery session {SessionId} stopped successfully", session.SessionId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping discovery session {SessionId}", session?.SessionId);
            return Task.FromResult(Result.Failure($"Failed to stop discovery session: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> SubmitFeedbackAsync(DiscoveryFeedback feedback, CancellationToken ct = default)
    {
        try
        {
            if (feedback == null)
                return Task.FromResult(Result.Failure("Feedback cannot be null", ErrorType.Validation));

            // Store feedback for learning
            var key = $"{feedback.Address:X}_{feedback.CorrectCategory ?? "Unknown"}";
            
            if (!_feedbackHistory.TryGetValue(key, out var history))
            {
                history = new HeuristicFeedbackData();
                _feedbackHistory[key] = history;
            }

            history.TotalSubmissions++;
            if (feedback.WasCorrect)
            {
                history.CorrectIdentifications++;
            }

            if (!string.IsNullOrEmpty(feedback.CorrectName))
            {
                history.UserProvidedNames[feedback.CorrectName] =
                    history.UserProvidedNames.GetValueOrDefault(feedback.CorrectName) + 1;
            }

            if (!string.IsNullOrEmpty(feedback.CorrectCategory))
            {
                history.UserProvidedCategories[feedback.CorrectCategory] =
                    history.UserProvidedCategories.GetValueOrDefault(feedback.CorrectCategory) + 1;
            }

            _logger.LogInformation(
                "Feedback submitted for address {Address}: WasCorrect={WasCorrect}, Category={Category}, Name={Name}",
                feedback.Address, 
                feedback.WasCorrect, 
                feedback.CorrectCategory,
                feedback.CorrectName ?? "(not provided)");

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback");
            return Task.FromResult(Result.Failure($"Failed to submit feedback: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Performs a discovery pass - scans memory and updates candidates.
    /// </summary>
    private async Task PerformDiscoveryPassAsync(DiscoverySession session, DiscoverySessionContext context, PlayerAction action, CancellationToken ct)
    {
        session.CurrentPass++;
        _logger.LogDebug("Starting discovery pass {Pass} for session {SessionId}", session.CurrentPass, session.SessionId);

        // Pass 1: Initial scan (if first pass)
        if (session.CurrentPass == 1)
        {
            await PerformInitialScanAsync(session, context, ct).ConfigureAwait(false);
        }
        else
        {
            // Subsequent passes: monitor for changes
            await MonitorForChangesAsync(session, context, action, ct).ConfigureAwait(false);
        }

        // Small delay to prevent overwhelming the system
        await Task.Delay(session.Options.ScanIntervalMs, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the initial memory scan to find potential candidates.
    /// </summary>
    private async Task PerformInitialScanAsync(DiscoverySession session, DiscoverySessionContext context, CancellationToken ct)
    {
        _logger.LogDebug("Performing initial memory scan for process {ProcessId}", session.ProcessId);

        var newCandidates = new List<DiscoveredValue>();

        // Define scan ranges based on options
        var scanRanges = GetScanRanges(session.Options);

        foreach (var range in scanRanges)
        {
            ct.ThrowIfCancellationRequested();

            // Scan for integers
            if (session.Options.ScanIntegers)
            {
                await ScanRangeForIntegersAsync(context.ProcessHandle, range.Start, range.Size, newCandidates, ct).ConfigureAwait(false);
            }

            // Scan for floats
            if (session.Options.ScanFloats)
            {
                await ScanRangeForFloatsAsync(context.ProcessHandle, range.Start, range.Size, newCandidates, ct).ConfigureAwait(false);
            }

            // Yield to prevent blocking
            await Task.Yield();
        }

        // Initialize candidates with first observation
        foreach (var candidate in newCandidates)
        {
            candidate.FirstObserved = _timeProvider.UtcNow;
            candidate.LastObserved = _timeProvider.UtcNow;
            candidate.ObservationCount = 1;

            candidate.ObservationHistory.Add(new ValueObservation
            {
                Timestamp = _timeProvider.UtcNow,
                Value = candidate.CurrentValue
            });

            // Apply initial heuristics
            ApplyInitialHeuristicScoring(candidate);
        }

        session.Candidates.AddRange(newCandidates);

        _logger.LogInformation("Initial scan found {Count} candidates for session {SessionId}", newCandidates.Count, session.SessionId);
    }

    /// <summary>
    /// Monitors existing candidates for changes based on player action.
    /// </summary>
    private async Task MonitorForChangesAsync(DiscoverySession session, DiscoverySessionContext context, PlayerAction action, CancellationToken ct)
    {
        var updatedCandidates = new List<DiscoveredValue>();
        var checkedCount = 0;
        var changedCount = 0;

        foreach (var candidate in session.Candidates.ToList())
        {
            ct.ThrowIfCancellationRequested();

            // Read current value
            var newValue = ReadValueAtAddress(context.ProcessHandle, candidate.Address, candidate.ValueType);
            if (newValue == null)
                continue;

            checkedCount++;

            // Check if value changed
            var previousValue = candidate.CurrentValue;
            var hasChanged = !ValuesEqual(previousValue, newValue);
            
            if (hasChanged)
                changedCount++;

            // Update candidate
            candidate.PreviousValue = previousValue;
            candidate.CurrentValue = newValue;
            candidate.LastObserved = _timeProvider.UtcNow;
            candidate.ObservationCount++;

            // Calculate delta
            double? delta = null;
            if (previousValue != null && newValue != null)
            {
                delta = CalculateDelta(previousValue, newValue);
            }

            // Record observation
            candidate.ObservationHistory.Add(new ValueObservation
            {
                Timestamp = _timeProvider.UtcNow,
                Value = newValue,
                RelatedAction = action,
                Delta = delta
            });

            // Filter based on action
            if (ShouldKeepCandidateAfterAction(candidate, action, hasChanged))
            {
                updatedCandidates.Add(candidate);
            }
        }

        // Replace candidates with filtered list
        session.Candidates.Clear();
        session.Candidates.AddRange(updatedCandidates);
        
        _logger.LogDebug(
            "Monitor pass complete for session {SessionId}. Checked: {Checked}, Changed: {Changed}, Remaining: {Remaining}",
            session.SessionId,
            checkedCount,
            changedCount,
            updatedCandidates.Count);
    }

    /// <summary>
    /// Applies heuristics to all candidates and returns them ranked by confidence.
    /// </summary>
    private List<DiscoveredValue> ApplyHeuristicsAndRank(DiscoverySession session)
    {
        foreach (var candidate in session.Candidates)
        {
            // Run all applicable heuristics
            var bestHeuristic = _heuristics
                .Where(h => h.SupportsValueType(candidate.ValueType))
                .Select(h => new
                {
                    Heuristic = h,
                    Confidence = h.CalculateConfidence(candidate, candidate.ObservationHistory)
                })
                .OrderByDescending(h => h.Confidence)
                .FirstOrDefault();

            if (bestHeuristic != null)
            {
                candidate.ConfidenceScore = bestHeuristic.Confidence;
                candidate.Category = bestHeuristic.Heuristic.Category;
                candidate.SuggestedName = SuggestName(candidate);
            }
        }

        var ranked = session.Candidates
            .OrderByDescending(c => c.ConfidenceScore)
            .ToList();
            
        _logger.LogDebug(
            "Applied heuristics to {Count} candidates for session {SessionId}. Top confidence: {TopConfidence:P}",
            ranked.Count,
            session.SessionId,
            ranked.FirstOrDefault()?.ConfidenceScore ?? 0);

        return ranked;
    }

    /// <summary>
    /// Suggests a name for a discovered value based on its category and type.
    /// </summary>
    private string SuggestName(DiscoveredValue value)
    {
        var baseName = value.Category switch
        {
            "Health" => value.ValueType.ToLowerInvariant() == "float" ? "Health (Float)" : "Health",
            "Currency" => "Gold/Credits",
            "Ammo" => "Ammo Count",
            "Position" => "Player Position",
            "Experience" => "Experience Points",
            "Score" => "Score",
            "Timer" => "Timer",
            _ => $"Unknown ({value.ValueType})"
        };

        // Add address for uniqueness
        return $"{baseName} @ 0x{value.Address:X8}";
    }

    /// <summary>
    /// Applies initial heuristic scoring to a new candidate.
    /// </summary>
    private void ApplyInitialHeuristicScoring(DiscoveredValue candidate)
    {
        var bestHeuristic = _heuristics
            .Where(h => h.SupportsValueType(candidate.ValueType))
            .Select(h => new
            {
                Heuristic = h,
                Confidence = h.CalculateConfidence(candidate, candidate.ObservationHistory)
            })
            .OrderByDescending(h => h.Confidence)
            .FirstOrDefault();

        if (bestHeuristic != null)
        {
            candidate.ConfidenceScore = bestHeuristic.Confidence * 0.5; // Initial lower confidence
            candidate.Category = bestHeuristic.Heuristic.Category;
            candidate.SuggestedName = SuggestName(candidate);
        }
    }

    /// <summary>
    /// Determines if a candidate should be kept after an action.
    /// </summary>
    private static bool ShouldKeepCandidateAfterAction(DiscoveredValue candidate, PlayerAction action, bool hasChanged)
    {
        return action switch
        {
            // For damage, expect health to decrease
            PlayerAction.TookDamage => candidate.Category == "Health" && ValueDecreased(candidate),

            // For healing, expect health to increase
            PlayerAction.Healed => candidate.Category == "Health" && ValueIncreased(candidate),

            // For spending, expect currency to decrease
            PlayerAction.SpentMoney => candidate.Category == "Currency" && ValueDecreased(candidate),

            // For earning, expect currency to increase
            PlayerAction.EarnedMoney => candidate.Category == "Currency" && ValueIncreased(candidate),

            // For ammo use, expect ammo to decrease
            PlayerAction.UsedAmmo => candidate.Category == "Ammo" && ValueDecreased(candidate),

            // For reload, expect ammo to increase
            PlayerAction.Reloaded => candidate.Category == "Ammo" && ValueIncreased(candidate),

            // For XP gain, expect XP to increase
            PlayerAction.GainedXp => candidate.Category == "Experience" && ValueIncreased(candidate),

            // For position changes, position should change
            PlayerAction.PositionChanged => candidate.Category == "Position" && hasChanged,

            // For score increases, score should increase
            PlayerAction.ScoreIncreased => candidate.Category == "Score" && ValueIncreased(candidate),

            // Default: keep if changed
            _ => true
        };
    }

    private static bool ValueDecreased(DiscoveredValue candidate)
    {
        if (candidate.PreviousValue == null || candidate.CurrentValue == null)
            return false;

        var delta = CalculateDelta(candidate.PreviousValue, candidate.CurrentValue);
        return delta.HasValue && delta.Value < 0;
    }

    private static bool ValueIncreased(DiscoveredValue candidate)
    {
        if (candidate.PreviousValue == null || candidate.CurrentValue == null)
            return false;

        var delta = CalculateDelta(candidate.PreviousValue, candidate.CurrentValue);
        return delta.HasValue && delta.Value > 0;
    }

    private static double? CalculateDelta(object previous, object current)
    {
        try
        {
            var prev = Convert.ToDouble(previous);
            var curr = Convert.ToDouble(current);
            return curr - prev;
        }
        catch
        {
            return null;
        }
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        try
        {
            var da = Convert.ToDouble(a);
            var db = Convert.ToDouble(b);
            return Math.Abs(da - db) < 0.0001;
        }
        catch
        {
            return a.Equals(b);
        }
    }

    /// <summary>
    /// Gets memory scan ranges based on options.
    /// </summary>
    private static List<MemoryRange> GetScanRanges(DiscoveryOptions options)
    {
        var ranges = new List<MemoryRange>();

        // Get system info for memory bounds
        GetSystemInfo(out var sysInfo);

        // Add common game memory ranges
        ranges.Add(new MemoryRange(options.ScanStartAddress, Math.Min(options.ScanSize, 0x01000000))); // First 16MB

        // Additional ranges for 32-bit games
        if (options.ScanSize > 0x01000000)
        {
            ranges.Add(new MemoryRange(0x10000000, Math.Min(options.ScanSize - 0x01000000, 0x10000000))); // 256MB-512MB
        }

        return ranges;
    }

    /// <summary>
    /// Scans a memory range for integer values.
    /// </summary>
    private Task ScanRangeForIntegersAsync(IntPtr processHandle, nuint startAddress, nuint size, List<DiscoveredValue> candidates, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            const int bufferSize = 4096; // Read 4KB at a time
            var buffer = new byte[bufferSize];

            for (nuint offset = 0; offset < size; offset += (nuint)bufferSize)
            {
                ct.ThrowIfCancellationRequested();

                var address = (IntPtr)(startAddress + offset);

                if (!ReadProcessMemory(processHandle, address, buffer, bufferSize, out var bytesRead) || bytesRead == 0)
                    continue;

                // Scan for integers in common ranges
                for (int i = 0; i < bytesRead - 4; i += 4)
                {
                    var value = BitConverter.ToInt32(buffer, i);

                    if (IsCommonIntegerValue(value))
                    {
                        var valueAddress = IntPtr.Add(address, i);
                        candidates.Add(new DiscoveredValue
                        {
                            Address = valueAddress,
                            ValueType = "Int32",
                            CurrentValue = value
                        });
                    }
                }

                // Limit candidates to prevent memory issues
                if (candidates.Count >= 50000)
                    break;
            }
        }, ct);
    }

    /// <summary>
    /// Scans a memory range for float values.
    /// </summary>
    private Task ScanRangeForFloatsAsync(IntPtr processHandle, nuint startAddress, nuint size, List<DiscoveredValue> candidates, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            const int bufferSize = 4096;
            var buffer = new byte[bufferSize];

            for (nuint offset = 0; offset < size; offset += (nuint)bufferSize)
            {
                ct.ThrowIfCancellationRequested();

                var address = (IntPtr)(startAddress + offset);

                if (!ReadProcessMemory(processHandle, address, buffer, bufferSize, out var bytesRead) || bytesRead == 0)
                    continue;

                // Scan for floats in common ranges
                for (int i = 0; i < bytesRead - 4; i += 4)
                {
                    var value = BitConverter.ToSingle(buffer, i);

                    if (IsCommonFloatValue(value))
                    {
                        var valueAddress = IntPtr.Add(address, i);
                        candidates.Add(new DiscoveredValue
                        {
                            Address = valueAddress,
                            ValueType = "Float",
                            CurrentValue = value
                        });
                    }
                }

                if (candidates.Count >= 50000)
                    break;
            }
        }, ct);
    }

    /// <summary>
    /// Checks if an integer value is in a common game value range.
    /// </summary>
    private static bool IsCommonIntegerValue(int value)
    {
        // Health ranges: 1-10000
        if (value >= 1 && value <= 10000)
            return true;

        // Ammo ranges: 0-999
        if (value >= 0 && value <= 999)
            return true;

        // Currency ranges: 0-999999
        if (value >= 0 && value <= 999999)
            return true;

        // XP/Score ranges: 0-99999999
        if (value >= 0 && value <= 99999999)
            return true;

        return false;
    }

    /// <summary>
    /// Checks if a float value is in a common game value range.
    /// </summary>
    private static bool IsCommonFloatValue(float value)
    {
        // Position coordinates: -100000 to +100000
        if (value >= -100000 && value <= 100000 && value != 0)
            return true;

        // Health as float: 0.0-1000.0
        if (value >= 0 && value <= 1000)
            return true;

        // Timers: 0.0-86400.0 (24 hours in seconds)
        if (value >= 0 && value <= 86400)
            return true;

        return false;
    }

    /// <summary>
    /// Reads a value at the specified address.
    /// </summary>
    private object? ReadValueAtAddress(IntPtr processHandle, IntPtr address, string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();

        return normalizedType switch
        {
            "int32" or "int" => ReadInt32(processHandle, address),
            "float" or "single" => ReadFloat(processHandle, address),
            "int64" or "long" => ReadInt64(processHandle, address),
            "double" => ReadDouble(processHandle, address),
            "int16" or "short" => ReadInt16(processHandle, address),
            "byte" => ReadByte(processHandle, address),
            _ => ReadInt32(processHandle, address)
        };
    }

    private int? ReadInt32(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[4];
        return ReadProcessMemory(processHandle, address, buffer, 4, out var bytesRead) && bytesRead == 4
            ? BitConverter.ToInt32(buffer, 0)
            : null;
    }

    private float? ReadFloat(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[4];
        return ReadProcessMemory(processHandle, address, buffer, 4, out var bytesRead) && bytesRead == 4
            ? BitConverter.ToSingle(buffer, 0)
            : null;
    }

    private long? ReadInt64(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[8];
        return ReadProcessMemory(processHandle, address, buffer, 8, out var bytesRead) && bytesRead == 8
            ? BitConverter.ToInt64(buffer, 0)
            : null;
    }

    private double? ReadDouble(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[8];
        return ReadProcessMemory(processHandle, address, buffer, 8, out var bytesRead) && bytesRead == 8
            ? BitConverter.ToDouble(buffer, 0)
            : null;
    }

    private short? ReadInt16(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[2];
        return ReadProcessMemory(processHandle, address, buffer, 2, out var bytesRead) && bytesRead == 2
            ? BitConverter.ToInt16(buffer, 0)
            : null;
    }

    private byte? ReadByte(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[1];
        return ReadProcessMemory(processHandle, address, buffer, 1, out var bytesRead) && bytesRead == 1
            ? buffer[0]
            : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Stop all active sessions
        lock (_sessionLock)
        {
            foreach (var context in _activeSessions.Values)
            {
                if (context.ProcessHandle != IntPtr.Zero)
                {
                    CloseHandle(context.ProcessHandle);
                }
                context.Process?.Dispose();
                context.Session.IsActive = false;
            }
            _activeSessions.Clear();
        }
    }

    /// <summary>
    /// Context for an active discovery session.
    /// </summary>
    private sealed class DiscoverySessionContext
    {
        public required DiscoverySession Session { get; init; }
        public required IntPtr ProcessHandle { get; set; }
        public Process? Process { get; init; }
    }

    /// <summary>
    /// Represents a memory range for scanning.
    /// </summary>
    private readonly struct MemoryRange(nuint start, nuint size)
    {
        public nuint Start { get; } = start;
        public nuint Size { get; } = size;
    }

    /// <summary>
    /// Stores feedback data for learning.
    /// </summary>
    private sealed class HeuristicFeedbackData
    {
        public int TotalSubmissions { get; set; }
        public int CorrectIdentifications { get; set; }
        public Dictionary<string, int> UserProvidedNames { get; } = new();
        public Dictionary<string, int> UserProvidedCategories { get; } = new();
    }
}
