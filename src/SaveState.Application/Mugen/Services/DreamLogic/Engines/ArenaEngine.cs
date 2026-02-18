namespace SaveState.Application.Mugen.Services.DreamLogic.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.DreamLogic;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for managing dream arena state and operations.
/// </summary>
public class ArenaEngine
{
    private readonly ILogger<ArenaEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, DreamState> _arenaStates = new();

    public ArenaEngine(ILogger<ArenaEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Registers a new arena with initial state.
    /// </summary>
    public void RegisterArena(DreamArena arena, DreamState initialState)
    {
        _logger.LogInformation("Registering arena {ArenaId}", arena.ArenaId);
        _arenaStates[arena.ArenaId] = initialState;
    }

    /// <summary>
    /// Tries to get the state for an arena.
    /// </summary>
    public bool TryGetState(string arenaId, out DreamState? state)
    {
        return _arenaStates.TryGetValue(arenaId, out state);
    }

    /// <summary>
    /// Applies a geometry transformation to an arena.
    /// </summary>
    public Task ApplyGeometryTransformationAsync(string arenaId, ArenaGeometry geometry, float stabilityCost, CancellationToken ct = default)
    {
        if (_arenaStates.TryGetValue(arenaId, out var state) && state != null)
        {
            state.CurrentGeometry = geometry;
            state.StabilityIndex = Math.Max(0, state.StabilityIndex - stabilityCost);
            state.LastUpdated = _timeProvider.UtcNow;
            _logger.LogDebug("Applied geometry transformation to arena {ArenaId}", arenaId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies surreal effects to an arena.
    /// </summary>
    public Task ApplySurrealEffectsAsync(string arenaId, IReadOnlyList<SurrealEffect> effects, float stabilityCost, CancellationToken ct = default)
    {
        if (_arenaStates.TryGetValue(arenaId, out var state) && state != null)
        {
            var currentElements = state.ActiveSurrealElements?.ToList() ?? new List<SurrealElement>();

            foreach (var effect in effects)
            {
                currentElements.Add(new SurrealElement
                {
                    ElementId = Guid.NewGuid().ToString(),
                    ElementType = SurrealElementType.FloatingObject,
                    Intensity = effect.Parameters.ContainsKey("intensity") ? (float)effect.Parameters["intensity"] : 0.5f,
                    Duration = effect.Duration,
                    CreatedAt = _timeProvider.UtcNow
                });
            }

            state.ActiveSurrealElements = currentElements;
            state.StabilityIndex = Math.Max(0, state.StabilityIndex - stabilityCost);
            state.LastUpdated = _timeProvider.UtcNow;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds a symbolic manifestation to an arena.
    /// </summary>
    public Task AddSymbolicManifestationAsync(string arenaId, SymbolicElement element, CancellationToken ct = default)
    {
        if (_arenaStates.TryGetValue(arenaId, out var state) && state != null)
        {
            var manifestations = state.SymbolicManifestations?.ToList() ?? new List<SymbolicElement>();
            manifestations.Add(element);
            state.SymbolicManifestations = manifestations;
            state.LastUpdated = _timeProvider.UtcNow;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates and returns the current dream state.
    /// </summary>
    public Task<DreamState> UpdateDreamStateAsync(string arenaId, CancellationToken ct = default)
    {
        if (_arenaStates.TryGetValue(arenaId, out var state) && state != null)
        {
            state.LastUpdated = _timeProvider.UtcNow;
            return Task.FromResult(state);
        }

        throw new InvalidOperationException($"Arena {arenaId} not found");
    }

    /// <summary>
    /// Calculates the instability level of an arena.
    /// </summary>
    public Task<ArenaInstability> CalculateInstabilityAsync(string arenaId, CancellationToken ct = default)
    {
        if (!_arenaStates.TryGetValue(arenaId, out var state) || state == null)
        {
            throw new InvalidOperationException($"Arena {arenaId} not found");
        }

        var stabilityIndex = state.StabilityIndex;
        var riskLevel = stabilityIndex switch
        {
            < 0.2f => DreamRiskLevel.Critical,
            < 0.4f => DreamRiskLevel.High,
            < 0.6f => DreamRiskLevel.Medium,
            < 0.8f => DreamRiskLevel.Low,
            _ => DreamRiskLevel.Low
        };

        var instability = new ArenaInstability
        {
            ArenaId = arenaId,
            StabilityIndex = stabilityIndex,
            InstabilityFactors = GenerateInstabilityFactors(state),
            DreamRiskLevel = riskLevel,
            EstimatedCollapseTime = stabilityIndex < 0.2f
                ? TimeSpan.FromMinutes(5)
                : TimeSpan.FromMinutes((stabilityIndex * 30)),
            MitigationStrategies = GenerateMitigationStrategies(riskLevel),
            LastAssessed = _timeProvider.UtcNow
        };

        return Task.FromResult(instability);
    }

    /// <summary>
    /// Triggers emergency stabilization for an arena.
    /// </summary>
    public Task TriggerEmergencyStabilizationAsync(string arenaId, float stabilizationAmount, CancellationToken ct = default)
    {
        if (_arenaStates.TryGetValue(arenaId, out var state) && state != null)
        {
            state.StabilityIndex = Math.Min(1.0f, state.StabilityIndex + stabilizationAmount);
            state.LastUpdated = _timeProvider.UtcNow;
            _logger.LogInformation("Emergency stabilization triggered for arena {ArenaId}. New stability: {Stability}",
                arenaId, state.StabilityIndex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Generates analytics for an arena.
    /// </summary>
    public Task<Models.DreamLogic.DreamAnalytics> GenerateAnalyticsAsync(string arenaId, TimeSpan period, CancellationToken ct = default)
    {
        if (!_arenaStates.TryGetValue(arenaId, out var state) || state == null)
        {
            throw new InvalidOperationException($"Arena {arenaId} not found");
        }

        var analytics = new Models.DreamLogic.DreamAnalytics
        {
            ArenaId = arenaId,
            Period = period,
            GeneratedAt = _timeProvider.UtcNow,
            TotalSurrealEvents = state.ActiveSurrealElements?.Count ?? 0,
            GeometryTransformations = 0,
            SymbolicManifestations = state.SymbolicManifestations?.Count ?? 0,
            CollectiveDreamsHosted = 0,
            AverageStability = state.StabilityIndex,
            MostCommonSurrealEvent = "FloatingObject",
            PlayerEmotionalImpact = new EmotionalImpact
            {
                AverageEmotionalIntensity = 0.7f,
                MostCommonEmotion = "Wonder",
                EmotionalVariety = 0.5f,
                PositiveEmotionalRatio = 0.8f
            },
            DreamCoherenceIndex = state.StabilityIndex * 0.9f
        };

        return Task.FromResult(analytics);
    }

    private static List<string> GenerateInstabilityFactors(DreamState state)
    {
        var factors = new List<string>();

        if (state.ActiveSurrealElements?.Count > 5)
            factors.Add("Too many surreal elements");

        if (state.StabilityIndex < 0.3f)
            factors.Add("Critical stability degradation");

        if (state.SymbolicManifestations?.Count > 10)
            factors.Add("Symbolic overload");

        return factors;
    }

    private static List<string> GenerateMitigationStrategies(DreamRiskLevel riskLevel)
    {
        return riskLevel switch
        {
            DreamRiskLevel.Critical => new List<string> { "Immediate stabilization", "Remove excess elements", "Emergency shutdown" },
            DreamRiskLevel.High => new List<string> { "Increase stability", "Monitor closely", "Prepare fallback" },
            DreamRiskLevel.Medium => new List<string> { "Watch trends", "Balance elements" },
            _ => new List<string> { "Maintain current state" }
        };
    }
}
