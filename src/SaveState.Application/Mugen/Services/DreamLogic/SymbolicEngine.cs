using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.DreamLogic;
using SaveState.Core.Common.Services;
namespace SaveState.Application.Mugen.Services.DreamLogic;

/// <summary>
/// Engine for managing symbolic manifestations.
/// </summary>
public class SymbolicEngine
{
    private readonly ILogger<SymbolicEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public SymbolicEngine(ILogger<SymbolicEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<SymbolicElement> ManifestSymbolAsync(SymbolicRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Manifesting symbol of type {SymbolType}", request.SymbolType);

        var element = new SymbolicElement
        {
            ElementId = Guid.NewGuid().ToString(),
            SymbolType = request.SymbolType,
            RepresentedEmotion = GetEmotionForSymbol(request.SymbolType),
            Intensity = request.Intensity,
            Position = request.Position,
            ManifestedAt = _timeProvider.UtcNow
        };

        return Task.FromResult(element);
    }

    public Task<List<SymbolicElement>> InterpretDreamSymbolsAsync(List<SurrealElement> surrealElements, CancellationToken ct = default)
    {
        var symbols = surrealElements.Select(e => new SymbolicElement
        {
            ElementId = Guid.NewGuid().ToString(),
            SymbolType = SymbolType.Light,
            RepresentedEmotion = "wonder",
            Intensity = e.Intensity,
            Position = e.Position,
            ManifestedAt = _timeProvider.UtcNow
        }).ToList();

        return Task.FromResult(symbols);
    }

    public Task<float> CalculateSymbolResonanceAsync(SymbolicElement symbol, DreamState state, CancellationToken ct = default)
    {
        var emotionalAlignment = state.EmotionalResonance;
        var symbolPower = symbol.Intensity;
        return Task.FromResult(emotionalAlignment * symbolPower);
    }

    public async Task<SymbolicManifestation> CreateManifestationAsync(DreamState state, SymbolicRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating symbolic manifestation in arena {ArenaId}", state.ArenaId);

        var element = await ManifestSymbolAsync(request, ct).ConfigureAwait(false);
        var manifestation = new SymbolicManifestation
        {
            ManifestationId = Guid.NewGuid().ToString(),
            Element = element,
            TriggerCondition = request.TriggerCondition,
            Duration = request.Duration,
            CreatedAt = _timeProvider.UtcNow
        };

        return manifestation;
    }

    public async Task<SymbolicManifestation> CreateSymbolicBackgroundAsync(DreamState state, SymbolicRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating symbolic background in arena {ArenaId}: {SymbolType}", state.ArenaId, request.SymbolType);

        var manifestation = await CreateManifestationAsync(state, request, ct).ConfigureAwait(false);

        var manifestations = state.SymbolicManifestations?.ToList() ?? new List<SymbolicElement>();
        manifestations.Add(manifestation.Element);
        state.SymbolicManifestations = manifestations;

        return manifestation;
    }

    public Task<MemoryPalace> ConstructMemoryPalaceAsync(MemoryPalaceRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Constructing memory palace for player {PlayerId}", request.PlayerId);

        var rooms = request.Memories.Select((m, i) => new MemoryRoom
        {
            RoomId = Guid.NewGuid().ToString(),
            Memory = m,
            Position = new System.Numerics.Vector3(i * 10f, 0f, 0f),
            AssociatedEmotion = "nostalgia",
            RoomType = RoomType.MemoryChamber
        }).ToList();

        var palace = new MemoryPalace
        {
            PalaceId = Guid.NewGuid().ToString(),
            PlayerId = request.PlayerId,
            ArenaId = request.ArenaId,
            Rooms = rooms,
            Layout = PalaceLayout.Linear,
            ConstructedAt = _timeProvider.UtcNow
        };

        return Task.FromResult(palace);
    }

    private string GetEmotionForSymbol(SymbolType type)
    {
        return type switch
        {
            SymbolType.Heart => "love",
            SymbolType.Flame => "passion",
            SymbolType.Water => "calm",
            SymbolType.Light => "hope",
            SymbolType.Shadow => "fear",
            SymbolType.MemoryPalace => "nostalgia",
            _ => "neutral"
        };
    }
}

/// <summary>
/// Legacy alias for backward compatibility.
/// </summary>
public class DreamLogicArenaServiceSymbolicEngine : SymbolicEngine
{
    public DreamLogicArenaServiceSymbolicEngine(ILogger<SymbolicEngine> logger, ITimeProvider timeProvider) : base(logger, timeProvider) { }
}
