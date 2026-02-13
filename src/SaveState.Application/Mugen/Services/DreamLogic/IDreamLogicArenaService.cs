using SaveState.Core.Common;
using SaveState.Application.Mugen.Models.DreamLogic;

namespace SaveState.Application.Mugen.Services.DreamLogic;

/// <summary>
/// Interface for dream logic arena service.
/// </summary>
public interface IDreamLogicArenaService
{
    Task<Result<DreamArena>> GenerateDreamArenaAsync(DreamArenaRequest request, CancellationToken ct = default);
    Task<Result<ImpossibleGeometry>> ApplyImpossibleGeometryAsync(string arenaId, GeometryTransformationRequest request, CancellationToken ct = default);
    Task<Result<DreamState>> GetDreamStateAsync(string arenaId, CancellationToken ct = default);
    Task<Result<SymbolicElement>> ManifestSymbolAsync(string arenaId, SymbolicRequest request, CancellationToken ct = default);
    Task<Result<SurrealEvent>> TriggerSurrealEventAsync(string arenaId, SurrealEventType eventType, CancellationToken ct = default);
    Task<Result<MemoryPalace>> CreateMemoryPalaceAsync(MemoryPalaceRequest request, CancellationToken ct = default);
    Task<Result<CollectiveDream>> InitiateCollectiveDreamAsync(CollectiveDreamRequest request, CancellationToken ct = default);
    Task<Result<DreamAnalytics>> GetDreamAnalyticsAsync(string arenaId, TimeSpan period, CancellationToken ct = default);
}

/// <summary>
/// Legacy alias for backward compatibility.
/// </summary>
public interface DreamLogicArenaServiceIDreamLogicArenaService : IDreamLogicArenaService { }
