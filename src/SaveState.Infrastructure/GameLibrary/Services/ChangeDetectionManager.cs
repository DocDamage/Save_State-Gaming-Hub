using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Manages change detection and filtering based on player actions.
/// </summary>
public sealed class ChangeDetectionManager
{
    private readonly ILogger<ChangeDetectionManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly MemoryScanningManager _memoryScanningManager;

    public ChangeDetectionManager(
        ILogger<ChangeDetectionManager> logger,
        ITimeProvider timeProvider,
        MemoryScanningManager memoryScanningManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _memoryScanningManager = memoryScanningManager ?? throw new ArgumentNullException(nameof(memoryScanningManager));
    }

    /// <summary>
    /// Performs the initial memory scan to find potential candidates.
    /// </summary>
    public async Task PerformInitialScanAsync(DiscoverySession session, DiscoverySessionContext context, CancellationToken ct)
    {
        _logger.LogDebug("Performing initial memory scan for process {ProcessId}", session.ProcessId);

        var newCandidates = new List<DiscoveredValue>();

        // Define scan ranges based on options
        var scanRanges = MemoryScanningManager.GetScanRanges(session.Options);

        foreach (var range in scanRanges)
        {
            ct.ThrowIfCancellationRequested();

            // Scan for integers
            if (session.Options.ScanIntegers)
            {
                await _memoryScanningManager.ScanRangeForIntegersAsync(context.ProcessHandle, range.Start, range.Size, newCandidates, ct).ConfigureAwait(false);
            }

            // Scan for floats
            if (session.Options.ScanFloats)
            {
                await _memoryScanningManager.ScanRangeForFloatsAsync(context.ProcessHandle, range.Start, range.Size, newCandidates, ct).ConfigureAwait(false);
            }

            // Yield to prevent blocking
            await Task.Yield();
        }

        // Initialize candidates with first observation
        var now = _timeProvider.UtcNow;
        foreach (var candidate in newCandidates)
        {
            candidate.FirstObserved = now;
            candidate.LastObserved = now;
            candidate.ObservationCount = 1;

            candidate.ObservationHistory.Add(new ValueObservation
            {
                Timestamp = now,
                Value = candidate.CurrentValue
            });
        }

        session.Candidates.AddRange(newCandidates);

        _logger.LogInformation("Initial scan found {Count} candidates for session {SessionId}", newCandidates.Count, session.SessionId);
    }

    /// <summary>
    /// Monitors existing candidates for changes based on player action.
    /// </summary>
    public async Task MonitorForChangesAsync(DiscoverySession session, DiscoverySessionContext context, PlayerAction action, CancellationToken ct)
    {
        var updatedCandidates = new List<DiscoveredValue>();
        var checkedCount = 0;
        var changedCount = 0;

        foreach (var candidate in session.Candidates.ToList())
        {
            ct.ThrowIfCancellationRequested();

            // Read current value
            var newValue = _memoryScanningManager.ReadValueAtAddress(context.ProcessHandle, candidate.Address, candidate.ValueType);
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
}
