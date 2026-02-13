using SaveState.Core.Common;
using SaveState.Application.Mugen.Models.NarrativeMemory;

namespace SaveState.Application.Mugen.Services.NarrativeMemory;

/// <summary>
/// Narrative memory service providing memory crystals, alternate timelines,
/// crystal synthesis, and butterfly effect mechanics.
/// </summary>
public interface INarrativeMemoryService
{
    Task<Result<MemoryCrystal>> GenerateMemoryCrystalAsync(NarrativeMatchResult matchResult, CrystalGenerationRequest request, CancellationToken ct = default);
    Task<Result<AlternateTimeline>> CreateAlternateTimelineAsync(string crystalId, TimelineBranchRequest request, CancellationToken ct = default);
    Task<Result<SynthesizedMove>> SynthesizeCrystalMoveAsync(CrystalSynthesisRequest request, CancellationToken ct = default);
    Task<Result<ButterflyEffect>> TriggerButterflyEffectAsync(string crystalId, ButterflyEffectRequest request, CancellationToken ct = default);
    Task<Result<CrystalCollection>> GetCrystalCollectionAsync(string playerId, CancellationToken ct = default);
    Task<Result<TimelineReplay>> ReplayTimelineAsync(string timelineId, ReplayRequest request, CancellationToken ct = default);
    Task<Result<CrystalEconomy>> GetCrystalEconomyAsync(string playerId, CancellationToken ct = default);
    Task<Result<MemoryCrystal>> EnhanceCrystalAsync(string crystalId, CrystalEnhancementRequest request, CancellationToken ct = default);
    Task<Result<CrystalTrade>> InitiateCrystalTradeAsync(CrystalTradeRequest request, CancellationToken ct = default);
    Task<Result<NarrativeAnalytics>> GetNarrativeAnalyticsAsync(string playerId, TimeSpan period, CancellationToken ct = default);
}
