namespace SaveState.Application.Mugen.Services.AdvancedCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.AdvancedCombat;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Collections.Concurrent;

/// <summary>
/// Combo system engine for chain attacks.
/// </summary>
public class ComboEngine
{
    private readonly ILogger<ComboEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, ComboSequence> _combos = new();

    public ComboEngine(ILogger<ComboEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates a new combo sequence.
    /// </summary>
    public Task<Result<ComboSequence>> CreateComboAsync(ComboInputRequest request, CancellationToken ct = default)
    {
        var comboId = Guid.NewGuid().ToString();
        var combo = new ComboSequence
        {
            ComboId = comboId,
            SessionId = request.SessionId,
            Moves = new List<ComboMove>(),
            TotalDamage = 0,
            TotalHits = 0,
            StartedAt = _timeProvider.UtcNow,
            IsComplete = false
        };

        _combos[comboId] = combo;

        _logger.LogDebug("Combo created for session {SessionId}: {ComboId}", request.SessionId, comboId);
        return Task.FromResult(Result.Success(combo));
    }

    /// <summary>
    /// Validates a combo sequence.
    /// </summary>
    public Task<Result<ComboValidation>> ValidateComboAsync(ComboSequence combo, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var scaling = 1.0f;
        var damage = 0;

        for (var i = 0; i < combo.Moves.Count; i++)
        {
            var move = combo.Moves[i];
            scaling *= move.ScalingFactor;
            damage += (int)(move.Damage * scaling);

            if (i > 0 && move.StartupFrames > 30)
            {
                errors.Add($"Move {i + 1} is too slow to combo");
            }
        }

        var validation = new ComboValidation
        {
            IsValid = errors.Count == 0,
            ComboId = combo.ComboId,
            TotalDamage = damage,
            MoveCount = combo.Moves.Count,
            Type = DetermineComboType(combo),
            Errors = errors,
            ScalingApplied = scaling
        };

        return Task.FromResult(Result.Success(validation));
    }

    /// <summary>
    /// Adds a move to an existing combo.
    /// </summary>
    public Task<Result<ComboSequence>> AddMoveToComboAsync(string comboId, string moveName, CancellationToken ct = default)
    {
        if (!_combos.TryGetValue(comboId, out var combo))
        {
            return Task.FromResult(Result.Failure<ComboSequence>($"Combo {comboId} not found", ErrorType.NotFound));
        }

        var move = new ComboMove
        {
            MoveId = Guid.NewGuid().ToString(),
            MoveName = moveName,
            Damage = 50,
            FrameAdvantage = 8,
            StartupFrames = 12,
            ScalingFactor = 0.9f,
            Property = AttackProperty.Mid,
            IsJuggleStarter = false,
            IsLauncher = moveName.Contains("Launcher"),
            ExecutedAt = _timeProvider.UtcNow
        };

        var moves = combo.Moves.ToList();
        moves.Add(move);
        combo.Moves = moves;
        combo.TotalHits++;
        combo.TotalDamage += move.Damage;

        _logger.LogDebug("Move added to combo {ComboId}: {MoveName}", comboId, moveName);
        return Task.FromResult(Result.Success(combo));
    }

    /// <summary>
    /// Gets all combos for a session (used for analysis and lookup).
    /// </summary>
    public IReadOnlyList<ComboSequence> GetCombosForSession(string sessionId)
    {
        return _combos.Values.Where(c => c.SessionId == sessionId).ToList();
    }

    private static ComboType DetermineComboType(ComboSequence combo)
    {
        if (combo.Moves.Any(m => m.IsLauncher)) return ComboType.Juggle;
        if (combo.TotalHits > 10) return ComboType.Special;
        return ComboType.Normal;
    }
}
