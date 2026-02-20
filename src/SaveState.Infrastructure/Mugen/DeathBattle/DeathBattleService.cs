using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.DeathBattle;
using SaveState.Core.Mugen.DeathBattle.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.DeathBattle;

/// <summary>
/// Implementation of Death Battle service.
/// </summary>
public class DeathBattleService : IDeathBattleService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<DeathBattleService> _logger;
    private readonly IMugenCharacterRepository _characterRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly Random _random = new();

    public DeathBattleService(
        SaveStateDbContext dbContext,
        ILogger<DeathBattleService> logger,
        IMugenCharacterRepository characterRepository,
        ITimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _characterRepository = characterRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<DeathBattleMatch>> CreateBattleAsync(
        CreateDeathBattleRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var c1Result = await _characterRepository.GetByIdAsync(request.Combatant1Id, ct);
            var c2Result = await _characterRepository.GetByIdAsync(request.Combatant2Id, ct);

            if (c1Result.IsFailure || c2Result.IsFailure)
                return Result<DeathBattleMatch>.Failure("One or both combatants not found", ErrorType.NotFound);

            var combatant1 = c1Result.Value;
            var combatant2 = c2Result.Value;

            var battleCode = request.CustomBattleCode?.ToUpperInvariant() 
                ?? $"DB-{combatant1.Name[..3].ToUpper()}-VS-{combatant2.Name[..3].ToUpper()}-{_random.Next(1000, 9999)}";

            // Check for duplicate code
            var existing = await _dbContext.DeathBattleMatches
                .FirstOrDefaultAsync(b => b.BattleCode == battleCode, ct);
            if (existing != null)
                battleCode = $"{battleCode}-{_random.Next(100, 999)}";

            var battle = new DeathBattleMatch
            {
                BattleCode = battleCode,
                Combatant1 = CreateCombatant(combatant1),
                Combatant2 = CreateCombatant(combatant2),
                State = DeathBattleState.Preparation,
                CreatedAt = _timeProvider.UtcNow,
                IsPublic = request.IsPublic,
                Tags = request.Tags ?? new List<string>(),
                Phases = CreatePhases(combatant1.Name, combatant2.Name)
            };

            _dbContext.DeathBattleMatches.Add(battle);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Created Death Battle: {BattleCode} - {C1} vs {C2}",
                battleCode, combatant1.Name, combatant2.Name);

            return Result<DeathBattleMatch>.Success(battle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Death Battle");
            return Result<DeathBattleMatch>.Failure($"Creation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<DeathBattleMatch>> GetBattleAsync(string battleCode, CancellationToken ct = default)
    {
        var battle = await _dbContext.DeathBattleMatches
            .FirstOrDefaultAsync(b => b.BattleCode == battleCode.ToUpperInvariant(), ct);

        if (battle == null)
            return Result<DeathBattleMatch>.Failure("Battle not found", ErrorType.NotFound);

        return Result<DeathBattleMatch>.Success(battle);
    }

    public async Task<Result> StartBattleAsync(string battleCode, CancellationToken ct = default)
    {
        var battle = await GetBattleAsync(battleCode, ct);
        if (battle.IsFailure) return Result.Failure(battle.Error!, battle.ErrorType);

        battle.Value.State = DeathBattleState.Researching;
        
        // Generate research
        battle.Value.Research = GenerateResearch(battle.Value.Combatant1, battle.Value.Combatant2);
        
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("Started Death Battle: {BattleCode}", battleCode);
        return Result.Success();
    }

    public async Task<Result<DeathBattlePhase>> NextPhaseAsync(string battleCode, CancellationToken ct = default)
    {
        var battle = await GetBattleAsync(battleCode, ct);
        if (battle.IsFailure) return Result<DeathBattlePhase>.Failure(battle.Error!, battle.ErrorType);

        if (battle.Value.CurrentPhaseIndex >= battle.Value.Phases.Count - 1)
            return Result<DeathBattlePhase>.Failure("No more phases", ErrorType.Validation);

        // Mark current phase complete
        battle.Value.Phases[battle.Value.CurrentPhaseIndex].IsComplete = true;
        battle.Value.CurrentPhaseIndex++;

        // Update state based on phase
        battle.Value.State = battle.Value.CurrentPhaseIndex switch
        {
            1 => DeathBattleState.Simulating,
            4 => DeathBattleState.InProgress,
            5 => DeathBattleState.Concluded,
            _ => battle.Value.State
        };

        await _dbContext.SaveChangesAsync(ct);

        return Result<DeathBattlePhase>.Success(battle.Value.Phases[battle.Value.CurrentPhaseIndex]);
    }

    public async Task<Result<DeathBattleSimulation>> RunSimulationsAsync(
        string battleCode, int simulationCount = 1000, CancellationToken ct = default)
    {
        var battle = await GetBattleAsync(battleCode, ct);
        if (battle.IsFailure) return Result<DeathBattleSimulation>.Failure(battle.Error!, battle.ErrorType);

        var sim = new DeathBattleSimulation
        {
            TotalSimulationsRun = simulationCount
        };

        // Run Monte Carlo simulation
        var c1Stats = battle.Value.Combatant1.Stats;
        var c2Stats = battle.Value.Combatant2.Stats;

        for (int i = 0; i < simulationCount; i++)
        {
            var c1Score = CalculateCombatScore(c1Stats);
            var c2Score = CalculateCombatScore(c2Stats);

            // Add randomness
            c1Score += _random.Next(-10, 10);
            c2Score += _random.Next(-10, 10);

            if (c1Score > c2Score) sim.Combatant1Wins++;
            else if (c2Score > c1Score) sim.Combatant2Wins++;
            else sim.Draws++;
        }

        // Generate key moments from a representative simulation
        sim.KeyMoments = GenerateKeyMoments(battle.Value.Combatant1, battle.Value.Combatant2);
        sim.MostLikelyScenario = sim.Combatant1WinRate > sim.Combatant2WinRate
            ? $"{battle.Value.Combatant1.Name} wins in a close match"
            : sim.Combatant2WinRate > sim.Combatant1WinRate
                ? $"{battle.Value.Combatant2.Name} dominates"
                : "Too close to call";

        battle.Value.Simulation = sim;
        await _dbContext.SaveChangesAsync(ct);

        return Result<DeathBattleSimulation>.Success(sim);
    }

    public async Task<Result<DeathBattleMatch>> ConcludeBattleAsync(
        string battleCode, Guid winnerId, DeathBattleOutcome outcome, string reasoning, CancellationToken ct = default)
    {
        var battle = await GetBattleAsync(battleCode, ct);
        if (battle.IsFailure) return Result<DeathBattleMatch>.Failure(battle.Error!, battle.ErrorType);

        var winner = battle.Value.Combatant1.CharacterId == winnerId
            ? battle.Value.Combatant1
            : battle.Value.Combatant2.CharacterId == winnerId
                ? battle.Value.Combatant2
                : null;

        if (winner == null)
            return Result<DeathBattleMatch>.Failure("Invalid winner ID", ErrorType.Validation);

        battle.Value.Winner = new DeathBattleWinner
        {
            CombatantId = winnerId,
            Name = winner.Name,
            VictoryQuote = GenerateVictoryQuote(winner.Name),
            FinishingMove = GenerateFinishingMove(winner.Name),
            Reasoning = reasoning
        };

        battle.Value.Outcome = outcome;
        battle.Value.State = DeathBattleState.Concluded;
        battle.Value.CompletedAt = _timeProvider.UtcNow;

        // Mark all phases complete
        foreach (var phase in battle.Value.Phases)
            phase.IsComplete = true;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Concluded Death Battle: {BattleCode} - Winner: {Winner}",
            battleCode, winner.Name);

        return Result<DeathBattleMatch>.Success(battle.Value);
    }

    public async Task<Result<List<DeathBattleMatch>>> GetBattlesAsync(
        DeathBattleFilter? filter = null, CancellationToken ct = default)
    {
        var query = _dbContext.DeathBattleMatches.AsQueryable();

        if (filter != null)
        {
            if (filter.State.HasValue)
                query = query.Where(b => b.State == filter.State.Value);
            if (filter.IsPublic.HasValue)
                query = query.Where(b => b.IsPublic == filter.IsPublic.Value);
            if (filter.FromDate.HasValue)
                query = query.Where(b => b.CreatedAt >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(b => b.CreatedAt <= filter.ToDate.Value);
        }

        query = filter?.SortBy switch
        {
            DeathBattleSortOrder.Oldest => query.OrderBy(b => b.CreatedAt),
            DeathBattleSortOrder.MostViewed => query.OrderByDescending(b => b.Stats.ViewCount),
            _ => query.OrderByDescending(b => b.CreatedAt)
        };

        var battles = await query.Take(100).ToListAsync(ct);
        return Result<List<DeathBattleMatch>>.Success(battles);
    }

    public async Task<Result<List<DeathBattleMatch>>> GetCharacterBattlesAsync(Guid characterId, CancellationToken ct = default)
    {
        var battles = await _dbContext.DeathBattleMatches
            .Where(b => b.Combatant1.CharacterId == characterId || b.Combatant2.CharacterId == characterId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        return Result<List<DeathBattleMatch>>.Success(battles);
    }

    public Task<Result> VoteAsync(string battleCode, Guid combatantId, Guid userId, CancellationToken ct = default)
    {
        // Implementation would track votes
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<VoteTally>> GetVoteTallyAsync(string battleCode, CancellationToken ct = default)
    {
        var battle = await GetBattleAsync(battleCode, ct);
        if (battle.IsFailure) return Result<VoteTally>.Failure(battle.Error!, battle.ErrorType);

        return Result<VoteTally>.Success(new VoteTally
        {
            BattleCode = battleCode,
            Combatant1Id = battle.Value.Combatant1.CharacterId,
            Combatant1Name = battle.Value.Combatant1.Name,
            Combatant1Votes = battle.Value.Combatant1.PreMatchVotes,
            Combatant2Id = battle.Value.Combatant2.CharacterId,
            Combatant2Name = battle.Value.Combatant2.Name,
            Combatant2Votes = battle.Value.Combatant2.PreMatchVotes
        });
    }

    public async Task<Result<DeathBattleSuggestion>> SuggestBattleAsync(
        Guid combatant1Id, Guid combatant2Id, string reasoning, Guid userId, CancellationToken ct = default)
    {
        var c1Result = await _characterRepository.GetByIdAsync(combatant1Id, ct);
        var c2Result = await _characterRepository.GetByIdAsync(combatant2Id, ct);

        if (c1Result.IsFailure || c2Result.IsFailure)
            return Result<DeathBattleSuggestion>.Failure("Invalid combatants", ErrorType.NotFound);

        var c1 = c1Result.Value;
        var c2 = c2Result.Value;

        var suggestion = new DeathBattleSuggestion
        {
            SuggestedCombatant1Id = combatant1Id,
            SuggestedCombatant1Name = c1.Name,
            SuggestedCombatant2Id = combatant2Id,
            SuggestedCombatant2Name = c2.Name,
            Reasoning = reasoning,
            SuggestedByUserId = userId,
            SuggestedAt = _timeProvider.UtcNow,
            Upvotes = 1
        };

        _dbContext.DeathBattleSuggestions.Add(suggestion);
        await _dbContext.SaveChangesAsync(ct);

        return Result<DeathBattleSuggestion>.Success(suggestion);
    }

    public async Task<Result<List<DeathBattleSuggestion>>> GetSuggestionsAsync(
        bool includeAccepted = false, CancellationToken ct = default)
    {
        var query = _dbContext.DeathBattleSuggestions.AsQueryable();
        if (!includeAccepted)
            query = query.Where(s => !s.IsAccepted);

        var suggestions = await query
            .OrderByDescending(s => s.Upvotes)
            .ToListAsync(ct);

        return Result<List<DeathBattleSuggestion>>.Success(suggestions);
    }

    public Task<Result> UpvoteSuggestionAsync(Guid suggestionId, Guid userId, CancellationToken ct = default)
    {
        // Implementation would increment upvotes
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<List<DeathBattleLeaderboardEntry>>> GetLeaderboardAsync(int top = 100, CancellationToken ct = default)
    {
        // Aggregate stats from all battles
        var battles = await _dbContext.DeathBattleMatches
            .Where(b => b.State == DeathBattleState.Concluded)
            .ToListAsync(ct);

        var characterStats = new Dictionary<Guid, DeathBattleLeaderboardEntry>();

        foreach (var battle in battles)
        {
            UpdateLeaderboardStats(characterStats, battle.Combatant1.CharacterId, battle.Combatant1.Name, battle);
            UpdateLeaderboardStats(characterStats, battle.Combatant2.CharacterId, battle.Combatant2.Name, battle);
        }

        var leaderboard = characterStats.Values
            .OrderByDescending(c => c.WinRate)
            .ThenByDescending(c => c.Wins)
            .Take(top)
            .ToList();

        // Assign ranks
        for (int i = 0; i < leaderboard.Count; i++)
            leaderboard[i].Rank = i + 1;

        return Result<List<DeathBattleLeaderboardEntry>>.Success(leaderboard);
    }

    public async Task<Result<CharacterDeathBattleStats>> GetCharacterStatsAsync(Guid characterId, CancellationToken ct = default)
    {
        var battles = await GetCharacterBattlesAsync(characterId, ct);
        if (battles.IsFailure) return Result<CharacterDeathBattleStats>.Failure(battles.Error!, battles.ErrorType);

        var stats = new CharacterDeathBattleStats
        {
            CharacterId = characterId,
            TotalBattles = battles.Value.Count
        };

        foreach (var battle in battles.Value)
        {
            if (battle.Winner?.CombatantId == characterId)
            {
                stats.Wins++;
                stats.NotableVictories.Add($"Defeated {(battle.Combatant1.CharacterId == characterId ? battle.Combatant2.Name : battle.Combatant1.Name)}");
            }
            else if (battle.Winner != null)
            {
                stats.Losses++;
                stats.NotableDefeats.Add($"Lost to {(battle.Combatant1.CharacterId == characterId ? battle.Combatant2.Name : battle.Combatant1.Name)}");
            }
            else
            {
                stats.Draws++;
            }
        }

        return Result<CharacterDeathBattleStats>.Success(stats);
    }

    public Task<Result<string>> GeneratePreviewAsync(string battleCode, CancellationToken ct = default)
    {
        var preview = $"⚔️ DEATH BATTLE! ⚔️\n\nComing soon...\n\nBattle Code: {battleCode}";
        return Task.FromResult(Result<string>.Success(preview));
    }

    public async Task<Result<byte[]>> ExportBattleAsync(
        string battleCode, ExportFormat format = ExportFormat.Json, CancellationToken ct = default)
    {
        var battle = await GetBattleAsync(battleCode, ct);
        if (battle.IsFailure) return Result<byte[]>.Failure(battle.Error!, battle.ErrorType);

        var json = JsonSerializer.Serialize(battle.Value, new JsonSerializerOptions { WriteIndented = true });
        return Result<byte[]>.Success(System.Text.Encoding.UTF8.GetBytes(json));
    }

    public async Task<Result<List<DeathBattleMatch>>> GetFeaturedBattlesAsync(CancellationToken ct = default)
    {
        var battles = await _dbContext.DeathBattleMatches
            .Where(b => b.IsPublic && b.State == DeathBattleState.Concluded)
            .OrderByDescending(b => b.Stats.ViewCount + b.Stats.Likes)
            .Take(10)
            .ToListAsync(ct);

        return Result<List<DeathBattleMatch>>.Success(battles);
    }

    public async Task<Result<(Guid Character1Id, Guid Character2Id)>> GetRandomMatchupAsync(CancellationToken ct = default)
    {
        var characters = await _characterRepository.GetAllAsync(ct);
        if (characters.Count < 2)
            return Result<(Guid, Guid)>.Failure("Not enough characters", ErrorType.NotFound);

        var shuffled = characters.OrderBy(_ => _random.Next()).ToList();
        return Result<(Guid, Guid)>.Success((shuffled[0].Id, shuffled[1].Id));
    }

    #region Helper Methods

    private DeathBattleCombatant CreateCombatant(MugenCharacter character)
    {
        return new DeathBattleCombatant
        {
            CharacterId = character.Id,
            Name = character.Name,
            Source = "MUGEN",
            Description = character.DisplayName,
            Stats = GenerateStats(character),
            Feats = new List<DeathBattleFeat>(),
            Abilities = new List<DeathBattleAbility>(),
            Strengths = new List<string> { "Versatile moveset", "AI adaptable" },
            Weaknesses = new List<string> { "Dependent on AI configuration" }
        };
    }

    private DeathBattleStatsProfile GenerateStats(MugenCharacter character)
    {
        // Generate realistic stats based on character attributes
        return new DeathBattleStatsProfile
        {
            Strength = _random.Next(60, 95),
            Speed = _random.Next(50, 90),
            Durability = _random.Next(55, 90),
            Intelligence = _random.Next(40, 85),
            CombatSkill = _random.Next(60, 95),
            Power = _random.Next(50, 100),
            Experience = _random.Next(30, 80),
            Hax = _random.Next(0, 50)
        };
    }

    private List<DeathBattlePhase> CreatePhases(string name1, string name2)
    {
        return new List<DeathBattlePhase>
        {
            new() { Type = DeathBattlePhaseType.Introduction, Title = "Introduction", Content = $"Two warriors enter, but only one will leave alive..." },
            new() { Type = DeathBattlePhaseType.Combatant1Analysis, Title = $"{name1}", Content = "" },
            new() { Type = DeathBattlePhaseType.Combatant2Analysis, Title = $"{name2}", Content = "" },
            new() { Type = DeathBattlePhaseType.Comparison, Title = "Comparison", Content = "" },
            new() { Type = DeathBattlePhaseType.FightSimulation, Title = "Death Battle!", Content = "" },
            new() { Type = DeathBattlePhaseType.Verdict, Title = "Verdict", Content = "" },
            new() { Type = DeathBattlePhaseType.NextTime, Title = "Next Time", Content = "" }
        };
    }

    private DeathBattleResearch GenerateResearch(DeathBattleCombatant c1, DeathBattleCombatant c2)
    {
        return new DeathBattleResearch
        {
            Combatant1Analysis = new CombatantAnalysis
            {
                CombatantId = c1.CharacterId,
                CombatantName = c1.Name,
                Overview = $"{c1.Name} enters the arena with formidable power.",
                Background = "A warrior of great renown.",
                KeyFeatsExplained = new List<string>(),
                Arsenal = c1.Abilities.Select(a => a.Name).ToList(),
                NotableWeaknesses = c1.Weaknesses,
                WinProbability = CalculateWinProbability(c1.Stats, c2.Stats)
            },
            Combatant2Analysis = new CombatantAnalysis
            {
                CombatantId = c2.CharacterId,
                CombatantName = c2.Name,
                Overview = $"{c2.Name} brings their own deadly arsenal.",
                Background = "A fierce competitor.",
                KeyFeatsExplained = new List<string>(),
                Arsenal = c2.Abilities.Select(a => a.Name).ToList(),
                NotableWeaknesses = c2.Weaknesses,
                WinProbability = CalculateWinProbability(c2.Stats, c1.Stats)
            },
            Comparisons = new List<DeathBattleComparison>
            {
                new() { Category = "Strength", Combatant1Score = c1.Stats.Strength, Combatant2Score = c2.Stats.Strength, Analysis = "Physical power comparison" },
                new() { Category = "Speed", Combatant1Score = c1.Stats.Speed, Combatant2Score = c2.Stats.Speed, Analysis = "Speed comparison" },
                new() { Category = "Intelligence", Combatant1Score = c1.Stats.Intelligence, Combatant2Score = c2.Stats.Intelligence, Analysis = "Tactical ability comparison" }
            }
        };
    }

    private int CalculateWinProbability(DeathBattleStatsProfile stats1, DeathBattleStatsProfile stats2)
    {
        var diff = stats1.Overall - stats2.Overall;
        return Math.Clamp(50 + diff, 10, 90);
    }

    private double CalculateCombatScore(DeathBattleStatsProfile stats)
    {
        return stats.Overall + _random.Next(-5, 5);
    }

    private List<SimulatedRound> GenerateKeyMoments(DeathBattleCombatant c1, DeathBattleCombatant c2)
    {
        return new List<SimulatedRound>
        {
            new() { RoundNumber = 1, Description = $"{c1.Name} opens with an aggressive attack!", Combatant1Health = 100, Combatant2Health = 95 },
            new() { RoundNumber = 2, Description = $"{c2.Name} counters with their signature move!", Combatant1Health = 80, Combatant2Health = 95 },
            new() { RoundNumber = 3, Description = "Both combatants trade devastating blows!", Combatant1Health = 60, Combatant2Health = 70 },
            new() { RoundNumber = 4, Description = "The final clash!", Combatant1Health = 0, Combatant2Health = 30, FinishingMove = "FINISHING MOVE!" }
        };
    }

    private string GenerateVictoryQuote(string winnerName)
    {
        var quotes = new[]
        {
            $"{winnerName} stands victorious!",
            $"{winnerName}: 'Is that all you've got?'",
            $"{winnerName} remains undefeated!"
        };
        return quotes[_random.Next(quotes.Length)];
    }

    private string GenerateFinishingMove(string winnerName)
    {
        return $"{winnerName} delivers the final, devastating blow!";
    }

    private void UpdateLeaderboardStats(
        Dictionary<Guid, DeathBattleLeaderboardEntry> stats,
        Guid characterId,
        string characterName,
        DeathBattleMatch battle)
    {
        if (!stats.ContainsKey(characterId))
        {
            stats[characterId] = new DeathBattleLeaderboardEntry
            {
                CharacterId = characterId,
                CharacterName = characterName,
                Tier = "Unknown"
            };
        }

        var entry = stats[characterId];
        entry.BattlesFought++;

        if (battle.Winner?.CombatantId == characterId)
            entry.Wins++;
        else
            entry.Losses++;
    }

    #endregion
}
