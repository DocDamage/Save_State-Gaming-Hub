using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.DreamLogic;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.DreamLogic;

/// <summary>
/// Engine for managing collective dream experiences.
/// </summary>
public class CollectiveEngine
{
    private readonly ILogger<CollectiveEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public CollectiveEngine(ILogger<CollectiveEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<CollectiveDream> InitiateCollectiveDreamAsync(CollectiveDreamRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Initiating collective dream with {PlayerCount} players", request.PlayerIds.Count);

        var dream = new CollectiveDream
        {
            DreamId = Guid.NewGuid().ToString(),
            PlayerIds = request.PlayerIds,
            ArenaId = request.ArenaId,
            SharedEmotionalState = new DreamEmotionalState
            {
                CharacterId = "collective",
                PrimaryEmotion = DreamEmotion.Neutral,
                Intensity = 0.5f
            },
            ManifestedElements = new List<SymbolicElement>(),
            DreamTheme = DreamTheme.Collective,
            InitiatedAt = _timeProvider.UtcNow,
            Duration = request.Duration,
            CoherenceLevel = 1.0f
        };

        return Task.FromResult(dream);
    }

    public Task<float> CalculateCoherenceAsync(List<string> playerIds, CancellationToken ct = default)
    {
        // Calculate dream coherence based on player count and shared focus
        var baseCoherence = 1.0f;
        var playerPenalty = (playerIds.Count - 1) * 0.05f;
        return Task.FromResult(Math.Max(0.1f, baseCoherence - playerPenalty));
    }

    public Task<DreamEmotionalState> MergeEmotionalStatesAsync(List<DreamEmotionalState> states, CancellationToken ct = default)
    {
        if (states.Count == 0)
        {
            return Task.FromResult(new DreamEmotionalState
            {
                CharacterId = "collective",
                PrimaryEmotion = DreamEmotion.Neutral,
                Intensity = 0.0f
            });
        }

        var dominantEmotion = states.GroupBy(s => s.PrimaryEmotion)
            .OrderByDescending(g => g.Count())
            .First().Key;

        var averageIntensity = states.Average(s => s.Intensity);

        return Task.FromResult(new DreamEmotionalState
        {
            CharacterId = "collective",
            PrimaryEmotion = dominantEmotion,
            Intensity = averageIntensity
        });
    }

    public Task<bool> SynchronizePlayersAsync(string dreamId, List<string> playerIds, CancellationToken ct = default)
    {
        _logger.LogDebug("Synchronizing {PlayerCount} players in collective dream {DreamId}", playerIds.Count, dreamId);
        return Task.FromResult(true);
    }

    // Alias for backward compatibility
    public Task<CollectiveDream> InitiateDreamAsync(CollectiveDreamRequest request, CancellationToken ct = default)
    {
        return InitiateCollectiveDreamAsync(request, ct);
    }

    public Task ApplyToArenaStateAsync(DreamState arenaState, CollectiveDream collectiveDream, CancellationToken ct = default)
    {
        var symbolicManifestations = arenaState.SymbolicManifestations?.ToList() ?? new List<SymbolicElement>();
        symbolicManifestations.AddRange(collectiveDream.ManifestedElements);
        arenaState.SymbolicManifestations = symbolicManifestations;
        arenaState.EmotionalResonance = collectiveDream.SharedEmotionalState.Intensity;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Legacy alias for backward compatibility.
/// </summary>
public class DreamLogicArenaServiceCollectiveEngine : CollectiveEngine
{
    public DreamLogicArenaServiceCollectiveEngine(ILogger<CollectiveEngine> logger, ITimeProvider timeProvider) : base(logger, timeProvider) { }
}
