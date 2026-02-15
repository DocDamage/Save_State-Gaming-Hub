using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Models.NarrativeMemory;
using SaveState.Application.Mugen.Services.NarrativeMemory.Engines;

namespace SaveState.Application.Mugen.Services.NarrativeMemory;

/// <summary>
/// Narrative memory crystals service providing alternate timeline collection,
/// memory crystal synthesis, butterfly effect mechanics, and narrative replay systems.
/// </summary>
public class NarrativeMemoryService : INarrativeMemoryService
{
    private readonly ILogger<NarrativeMemoryService> _logger;
    private readonly ICacheService _cache;
    private readonly Dictionary<string, MemoryCrystal> _memoryCrystals = new();
    private readonly Dictionary<string, AlternateTimeline> _alternateTimelines = new();
    private readonly Dictionary<string, CrystalCollection> _crystalCollections = new();
    private readonly CrystalEngine _crystalEngine;
    private readonly TimelineEngine _timelineEngine;
    private readonly SynthesisEngine _synthesisEngine;
    private readonly ButterflyEngine _butterflyEngine;

    public NarrativeMemoryService(
        ILogger<NarrativeMemoryService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache)
    {
        _logger = logger;
        _cache = cache;
        _crystalEngine = new CrystalEngine(loggerFactory.CreateLogger<CrystalEngine>());
        _timelineEngine = new TimelineEngine(loggerFactory.CreateLogger<TimelineEngine>());
        _synthesisEngine = new SynthesisEngine(loggerFactory.CreateLogger<SynthesisEngine>());
        _butterflyEngine = new ButterflyEngine(loggerFactory.CreateLogger<ButterflyEngine>());

        InitializeMemorySystem();
    }

    public async Task<Result<MemoryCrystal>> GenerateMemoryCrystalAsync(NarrativeMatchResult matchResult, CrystalGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating memory crystal for match {MatchId}", matchResult.MatchId);

            // Convert NarrativeMatchResult to MatchMemory
            var matchMemory = new MatchMemory
            {
                MatchId = matchResult.MatchId,
                RoundNumber = matchResult.RoundNumber,
                Outcome = matchResult.Outcome,
                DamageDealt = matchResult.DamageDealt,
                DamageReceived = matchResult.DamageReceived,
                Duration = matchResult.Duration,
                CombosUsed = matchResult.CombosUsed,
                EmotionalContext = matchResult.EmotionalContext
            };

            var crystal = await _crystalEngine.GenerateCrystalAsync(request.PlayerId, matchMemory, ct);
            _memoryCrystals[crystal.CrystalId] = crystal;
            await AddToCollectionAsync(request.PlayerId, crystal, ct);

            _logger.LogInformation("Memory crystal generated: {CrystalId} ({Rarity})", crystal.CrystalId, crystal.Rarity);
            return Result.Success<MemoryCrystal>(crystal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating memory crystal for match {MatchId}", matchResult.MatchId);
            return Result.Failure<MemoryCrystal>($"Memory crystal generation failed: {ex.Message}");
        }
    }

    public async Task<Result<AlternateTimeline>> CreateAlternateTimelineAsync(string crystalId, TimelineBranchRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!_memoryCrystals.TryGetValue(crystalId, out var crystal))
            {
                return Result.Failure<AlternateTimeline>("Memory crystal not found");
            }

            _logger.LogInformation("Creating alternate timeline from crystal {CrystalId}", crystalId);

            // Convert TimelineBranchRequest to TimelineForkRequest
            var forkRequest = new TimelineForkRequest
            {
                PlayerId = request.PlayerId,
                BranchPoint = request.BranchPoint,
                DesiredOutcome = request.DesiredOutcome,
                Probability = request.Probability
            };

            var timeline = await _timelineEngine.CreateAlternateTimelineAsync(request.PlayerId, forkRequest, ct);
            _alternateTimelines[timeline.TimelineId] = timeline;

            _logger.LogInformation("Alternate timeline created: {TimelineId}", timeline.TimelineId);
            return Result.Success<AlternateTimeline>(timeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alternate timeline from crystal {CrystalId}", crystalId);
            return Result.Failure<AlternateTimeline>($"Alternate timeline creation failed: {ex.Message}");
        }
    }

    public async Task<Result<SynthesizedMove>> SynthesizeCrystalMoveAsync(CrystalSynthesisRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Synthesizing move from {Count} crystals", request.CrystalIds.Count);

            var crystals = request.CrystalIds
                .Select(id => _memoryCrystals.GetValueOrDefault(id))
                .Where(c => c != null)
                .ToList();

            if (crystals.Count != request.CrystalIds.Count)
            {
                return Result.Failure<SynthesizedMove>("One or more memory crystals not found");
            }

            // Convert CrystalSynthesisRequest to MoveSynthesisRequest
            var moveRequest = new MoveSynthesisRequest
            {
                PlayerId = request.PlayerId,
                CrystalIds = request.CrystalIds,
                DesiredMoveType = request.DesiredMoveType
            };

            var synthesizedMove = await _synthesisEngine.SynthesizeMoveAsync(request.PlayerId, moveRequest, ct);

            _logger.LogInformation("Move synthesized: {MoveName} ({Power})", synthesizedMove.Name, synthesizedMove.Power);
            return Result.Success<SynthesizedMove>(synthesizedMove);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing move from crystals");
            return Result.Failure<SynthesizedMove>($"Move synthesis failed: {ex.Message}");
        }
    }

    public async Task<Result<ButterflyEffect>> TriggerButterflyEffectAsync(string crystalId, ButterflyEffectRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!_memoryCrystals.TryGetValue(crystalId, out var crystal))
            {
                return Result.Failure<ButterflyEffect>("Memory crystal not found");
            }

            _logger.LogInformation("Triggering butterfly effect from crystal {CrystalId}", crystalId);

            var effectResult = await _butterflyEngine.TriggerEffectAsync(crystal.PlayerId, request, ct);
            await ApplyCascadingChangesAsync(effectResult.Effect, ct);

            _logger.LogInformation("Butterfly effect triggered: {Magnitude:F2} magnitude affecting {CrystalCount} crystals",
                effectResult.Effect.Magnitude, effectResult.Effect.AffectedCrystals.Count);

            return Result.Success<ButterflyEffect>(effectResult.Effect);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering butterfly effect from crystal {CrystalId}", crystalId);
            return Result.Failure<ButterflyEffect>($"Butterfly effect failed: {ex.Message}");
        }
    }

    public async Task<Result<CrystalCollection>> GetCrystalCollectionAsync(string playerId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Retrieving crystal collection for player {PlayerId}", playerId);

            var collection = await GetOrCreateCollectionAsync(playerId, ct);
            return Result.Success<CrystalCollection>(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving crystal collection for player {PlayerId}", playerId);
            return Result.Failure<CrystalCollection>($"Collection retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result<TimelineReplay>> ReplayTimelineAsync(string timelineId, ReplayRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!_alternateTimelines.TryGetValue(timelineId, out var timeline))
            {
                return Result.Failure<TimelineReplay>("Timeline not found");
            }

            _logger.LogInformation("Replaying timeline {TimelineId}", timelineId);

            // Convert ReplayRequest to ReplayOptions
            var replayOptions = new ReplayOptions
            {
                PlayerId = timeline.CreatorId,
                IncludeCommentary = true,
                PlaybackSpeed = 1.0f
            };

            var replay = await _timelineEngine.ReplayTimelineAsync(timelineId, replayOptions, ct);

            _logger.LogInformation("Timeline replay completed: {ReplayId}", replay.ReplayId);
            return Result.Success<TimelineReplay>(replay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replaying timeline {TimelineId}", timelineId);
            return Result.Failure<TimelineReplay>($"Timeline replay failed: {ex.Message}");
        }
    }

    public async Task<Result<CrystalEconomy>> GetCrystalEconomyAsync(string playerId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating crystal economy for player {PlayerId}", playerId);

            var economy = new CrystalEconomy
            {
                PlayerId = playerId,
                TotalCrystals = _memoryCrystals.Values.Count(c => c.PlayerId == playerId),
                RareCrystals = _memoryCrystals.Values.Count(c => c.PlayerId == playerId && c.Rarity >= CrystalRarity.Rare),
                EpicCrystals = _memoryCrystals.Values.Count(c => c.PlayerId == playerId && c.Rarity >= CrystalRarity.Epic),
                LegendaryCrystals = _memoryCrystals.Values.Count(c => c.PlayerId == playerId && c.Rarity == CrystalRarity.Legendary),
                CrystalValue = CalculateCrystalValue(playerId),
                TradeOpportunities = await AnalyzeTradeOpportunitiesAsync(playerId, ct),
                SynthesisPotential = await AnalyzeSynthesisPotentialAsync(playerId, ct),
                MarketValue = CalculateMarketValue(playerId),
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Crystal economy generated: {TotalValue:F2} total value", economy.CrystalValue);
            return Result.Success<CrystalEconomy>(economy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating crystal economy for player {PlayerId}", playerId);
            return Result.Failure<CrystalEconomy>($"Economy generation failed: {ex.Message}");
        }
    }

    public async Task<Result<MemoryCrystal>> EnhanceCrystalAsync(string crystalId, CrystalEnhancementRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!_memoryCrystals.TryGetValue(crystalId, out var crystal))
            {
                return Result.Failure<MemoryCrystal>("Memory crystal not found");
            }

            _logger.LogInformation("Enhancing crystal {CrystalId} with {EnhancementType}", crystalId, request.EnhancementType);

            // Convert CrystalEnhancementRequest to EnhancementRequest
            var enhancementRequest = new EnhancementRequest
            {
                EnhancementType = request.EnhancementType,
                EnhancementStrength = request.EnhancementStrength
            };

            var enhancedCrystal = await _crystalEngine.EnhanceCrystalAsync(crystalId, enhancementRequest, ct);
            _memoryCrystals[crystalId] = enhancedCrystal;

            _logger.LogInformation("Crystal enhanced: new rarity {Rarity}", enhancedCrystal.Rarity);
            return Result.Success<MemoryCrystal>(enhancedCrystal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing crystal {CrystalId}", crystalId);
            return Result.Failure<MemoryCrystal>($"Crystal enhancement failed: {ex.Message}");
        }
    }

    public async Task<Result<CrystalTrade>> InitiateCrystalTradeAsync(CrystalTradeRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Initiating crystal trade between {SellerId} and {BuyerId}", request.SellerId, request.BuyerId);

            foreach (var crystalId in request.OfferedCrystals)
            {
                if (!_memoryCrystals.TryGetValue(crystalId, out var crystal) || crystal.PlayerId != request.SellerId)
                {
                    return Result.Failure<CrystalTrade>($"Crystal {crystalId} not owned by seller");
                }
            }

            var trade = new CrystalTrade
            {
                TradeId = Guid.NewGuid().ToString(),
                SellerId = request.SellerId,
                BuyerId = request.BuyerId,
                OfferedCrystals = request.OfferedCrystals,
                RequestedCrystals = request.RequestedCrystals,
                OfferedValue = CalculateTradeValue(request.OfferedCrystals),
                RequestedValue = CalculateTradeValue(request.RequestedCrystals),
                Status = TradeStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _logger.LogInformation("Crystal trade initiated: {TradeId}", trade.TradeId);
            return Result.Success<CrystalTrade>(trade);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating crystal trade");
            return Result.Failure<CrystalTrade>($"Trade initiation failed: {ex.Message}");
        }
    }

    public async Task<Result<NarrativeAnalytics>> GetNarrativeAnalyticsAsync(string playerId, TimeSpan period, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating narrative analytics for player {PlayerId}", playerId);

            var analytics = new NarrativeAnalytics
            {
                PlayerId = playerId,
                Period = period,
                CrystalsCollected = _memoryCrystals.Values.Count(c => c.PlayerId == playerId),
                TimelinesExplored = _alternateTimelines.Values.Count(t => t.CreatorId == playerId),
                MovesSynthesized = await CountSynthesizedMovesAsync(playerId, period, ct),
                ButterflyEffectsTriggered = await CountButterflyEffectsAsync(playerId, period, ct),
                NarrativeDiversity = CalculateNarrativeDiversity(playerId),
                StoryCompletion = CalculateStoryCompletion(playerId),
                AlternateOutcomesExplored = CalculateAlternateOutcomesExplored(playerId),
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Narrative analytics generated successfully");
            return Result.Success<NarrativeAnalytics>(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating narrative analytics for player {PlayerId}", playerId);
            return Result.Failure<NarrativeAnalytics>($"Analytics generation failed: {ex.Message}");
        }
    }

    #region Private Methods

    private void InitializeMemorySystem()
    {
        _logger.LogInformation("Narrative memory system initialized");
    }

    private async Task AddToCollectionAsync(string playerId, MemoryCrystal crystal, CancellationToken ct)
    {
        var collection = await GetOrCreateCollectionAsync(playerId, ct);
        collection.Crystals.Add(crystal.CrystalId);
        collection.TotalCrystals++;
        collection.LastUpdated = DateTime.UtcNow;
    }

    private async Task<CrystalCollection> GetOrCreateCollectionAsync(string playerId, CancellationToken ct)
    {
        await Task.CompletedTask;
        if (!_crystalCollections.TryGetValue(playerId, out var collection))
        {
            collection = new CrystalCollection
            {
                PlayerId = playerId,
                Crystals = new List<string>(),
                TotalCrystals = 0,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            _crystalCollections[playerId] = collection;
        }
        return collection;
    }

    private async Task ApplyCascadingChangesAsync(ButterflyEffect butterflyEffect, CancellationToken ct)
    {
        foreach (var crystalId in butterflyEffect.AffectedCrystals)
        {
            if (_memoryCrystals.TryGetValue(crystalId, out var crystal))
            {
                await Task.Delay(10, ct);
            }
        }
    }

    private decimal CalculateCrystalValue(string playerId)
    {
        return _memoryCrystals.Values
            .Where(c => c.PlayerId == playerId)
            .Sum(c => GetCrystalValue(c.Rarity));
    }

    private decimal GetCrystalValue(CrystalRarity rarity)
    {
        return rarity switch
        {
            CrystalRarity.Common => 1.0m,
            CrystalRarity.Uncommon => 5.0m,
            CrystalRarity.Rare => 25.0m,
            CrystalRarity.Epic => 125.0m,
            CrystalRarity.Legendary => 625.0m,
            _ => 1.0m
        };
    }

    private async Task<List<TradeOpportunity>> AnalyzeTradeOpportunitiesAsync(string playerId, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new List<TradeOpportunity>
        {
            new TradeOpportunity
            {
                OpportunityId = Guid.NewGuid().ToString(),
                OfferedCrystal = "rare_combo_crystal",
                RequestedCrystal = "epic_counter_crystal",
                ValueRatio = 1.8f,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            }
        };
    }

    private async Task<SynthesisPotential> AnalyzeSynthesisPotentialAsync(string playerId, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new SynthesisPotential
        {
            CompatibleCrystals = 15,
            PotentialMoves = 8,
            RarityUpgradeChance = 0.3f,
            UniqueCombinations = 24
        };
    }

    private decimal CalculateMarketValue(string playerId)
    {
        return CalculateCrystalValue(playerId) * 1.2m;
    }

    private decimal CalculateTradeValue(IReadOnlyList<string> crystalIds)
    {
        return crystalIds.Sum(id =>
        {
            if (_memoryCrystals.TryGetValue(id, out var crystal))
            {
                return GetCrystalValue(crystal.Rarity);
            }
            return 0m;
        });
    }

    private async Task<int> CountSynthesizedMovesAsync(string playerId, TimeSpan period, CancellationToken ct)
    {
        await Task.CompletedTask;
        return 12;
    }

    private async Task<int> CountButterflyEffectsAsync(string playerId, TimeSpan period, CancellationToken ct)
    {
        await Task.CompletedTask;
        return 5;
    }

    private float CalculateNarrativeDiversity(string playerId)
    {
        var uniqueOutcomes = _memoryCrystals.Values
            .Where(c => c.PlayerId == playerId)
            .Select(c => c.MatchOutcome)
            .Distinct()
            .Count();
        return Math.Min(uniqueOutcomes / 10.0f, 1.0f);
    }

    private float CalculateStoryCompletion(string playerId)
    {
        return 0.67f;
    }

    private int CalculateAlternateOutcomesExplored(string playerId)
    {
        return _alternateTimelines.Values.Count(t => t.CreatorId == playerId);
    }

    #endregion
}
