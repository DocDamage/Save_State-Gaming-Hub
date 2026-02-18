namespace SaveState.Application.Mugen.Services.DreamLogic.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.DreamLogic;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for creating and managing dream states.
/// </summary>
public class DreamEngine
{
    private readonly ILogger<DreamEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public DreamEngine(ILogger<DreamEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates an initial dream state for a new arena.
    /// </summary>
    public Task<DreamState> CreateInitialStateAsync(DreamArena arena, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating initial dream state for arena {ArenaId}", arena.ArenaId);

        var state = new DreamState
        {
            ArenaId = arena.ArenaId,
            CurrentGeometry = arena.BaseGeometry,
            ActiveSurrealElements = new List<SurrealElement>(),
            SymbolicManifestations = new List<SymbolicElement>(),
            EmotionalResonance = arena.EmotionalResonance,
            StabilityIndex = arena.StabilityRating,
            LastUpdated = _timeProvider.UtcNow
        };

        return Task.FromResult(state);
    }
}
