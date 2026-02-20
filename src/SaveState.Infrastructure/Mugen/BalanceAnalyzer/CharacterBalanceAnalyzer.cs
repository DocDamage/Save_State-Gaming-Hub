using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.BalanceAnalyzer;

/// <summary>
/// Implementation of character balance analyzer for MUGEN.
/// </summary>
public class CharacterBalanceAnalyzer : ICharacterBalanceAnalyzer
{
    private readonly ILogger<CharacterBalanceAnalyzer> _logger;
    private readonly IMugenMatchHistoryRepository _matchHistoryRepository;
    private readonly IMugenCharacterRepository _characterRepository;
    private readonly IMugenStatsService _statsService;
    private readonly ITimeProvider _timeProvider;

    public CharacterBalanceAnalyzer(
        ILogger<CharacterBalanceAnalyzer> logger,
        IMugenMatchHistoryRepository matchHistoryRepository,
        IMugenCharacterRepository characterRepository,
        IMugenStatsService statsService,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _matchHistoryRepository = matchHistoryRepository;
        _characterRepository = characterRepository;
        _statsService = statsService;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<Result<TierList>> GenerateTierListAsync(
        TierListGenerationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating tier list with criteria: {Criteria}", request.Criteria);

            var characters = await GetCharactersAsync(request.CharacterNames, ct);
            var placements = new List<CharacterTierPlacement>();

            foreach (var character in characters)
            {
                var winRates = await AnalyzeCharacterWinRatesAsync(character.Name, request.StartDate, request.EndDate, ct);
                if (winRates.IsFailure) continue;

                var score = CalculateTierScore(winRates.Value, request.Criteria);
                var matchups = request.IncludeMatchupData 
                    ? winRates.Value.MatchupWinRates.Select(m => new MatchupInfo(m.OpponentName, m.Wins, m.MatchesPlayed - m.Wins, m.WinRate)).ToList()
                    : new List<MatchupInfo>();

                placements.Add(new CharacterTierPlacement(
                    character.Name,
                    "TBD",
                    0,
                    score,
                    winRates.Value.TotalMatches,
                    winRates.Value.OverallWinRate,
                    matchups));
            }

            placements = placements.OrderByDescending(p => p.Score).ToList();
            var tiers = AssignTiers(placements);

            var tierList = new TierList(
                Guid.NewGuid().ToString(),
                _timeProvider.UtcNow,
                request.Criteria,
                placements.Sum(p => p.MatchesPlayed),
                tiers,
                placements);

            _logger.LogInformation("Tier list generated with {CharacterCount} characters", placements.Count);
            return Result<TierList>.Success(tierList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate tier list");
            return Result<TierList>.Failure($"Tier list generation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<CharacterWinRates>> AnalyzeCharacterWinRatesAsync(
        string characterName,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing win rates for: {Character}", characterName);

            var characterResult = await _characterRepository.GetByNameAsync(characterName, ct);
            if (characterResult.IsFailure)
            {
                return Result<CharacterWinRates>.Failure(
                    $"Character not found: {characterName}",
                    ErrorType.NotFound);
            }

            var characterId = characterResult.Value.Id;
            var matches = await _matchHistoryRepository.GetByCharacterAsync(characterId, limit: 1000, ct);
            
            // Filter by date if specified
            var filteredMatches = matches.Where(m => 
                (!startDate.HasValue || m.PlayedAt >= startDate.Value) &&
                (!endDate.HasValue || m.PlayedAt <= endDate.Value)).ToList();

            if (filteredMatches.Count == 0)
            {
                return Result<CharacterWinRates>.Failure(
                    $"No matches found for character: {characterName}",
                    ErrorType.NotFound);
            }

            int wins = 0, losses = 0, draws = 0;
            var matchupData = new Dictionary<Guid, (int Wins, int Losses)>();

            foreach (var match in filteredMatches)
            {
                bool isP1 = match.Player1CharacterId == characterId;
                Guid opponentId = isP1 ? match.Player2CharacterId : match.Player1CharacterId;
                bool isWinner = (isP1 && match.Result == MatchResult.Player1Win) ||
                               (!isP1 && match.Result == MatchResult.Player2Win);
                bool isDraw = match.Result == MatchResult.Draw;

                if (!matchupData.ContainsKey(opponentId))
                    matchupData[opponentId] = (0, 0);

                if (isDraw)
                {
                    draws++;
                }
                else if (isWinner)
                {
                    wins++;
                    matchupData[opponentId] = (matchupData[opponentId].Wins + 1, matchupData[opponentId].Losses);
                }
                else
                {
                    losses++;
                    matchupData[opponentId] = (matchupData[opponentId].Wins, matchupData[opponentId].Losses + 1);
                }
            }

            var totalMatches = wins + losses + draws;
            var winRate = totalMatches > 0 ? (double)wins / totalMatches * 100 : 0;

            // Build matchup win rates with opponent names
            var matchupWinRates = new List<MatchupWinRate>();
            foreach (var kvp in matchupData)
            {
                var opponentResult = await _characterRepository.GetByIdAsync(kvp.Key, ct);
                if (opponentResult.IsFailure) continue;

                var (w, l) = kvp.Value;
                var total = w + l;
                var rate = total > 0 ? (double)w / total * 100 : 0;
                
                matchupWinRates.Add(new MatchupWinRate(
                    opponentResult.Value.Name,
                    total,
                    w,
                    rate,
                    ClassifyAdvantage(rate)));
            }

            var trend = new WinRateTrend(winRate, winRate, winRate, SaveState.Core.Mugen.Services.TrendDirection.Stable);

            return Result<CharacterWinRates>.Success(new CharacterWinRates(
                characterName,
                totalMatches,
                wins,
                losses,
                draws,
                winRate,
                matchupWinRates,
                trend));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze win rates for: {Character}", characterName);
            return Result<CharacterWinRates>.Failure(
                $"Win rate analysis failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<MoveUsageStatistics>> GetMoveUsageStatisticsAsync(
        string characterName,
        int? minMatches = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting move usage statistics for: {Character}", characterName);

            // Get character ID
            var characterResult = await _characterRepository.GetByNameAsync(characterName, ct);
            if (characterResult.IsFailure)
            {
                return Result<MoveUsageStatistics>.Failure(
                    $"Character not found: {characterName}",
                    ErrorType.NotFound);
            }

            // Get character stats which may include move data
            var statsResult = await _statsService.GetCharacterStatsAsync(characterResult.Value.Id, ct);
            
            // Since we don't have direct move analytics, return placeholder data
            var usages = new List<MoveUsage>();
            
            return Result<MoveUsageStatistics>.Success(new MoveUsageStatistics(
                characterName,
                0,
                usages,
                new List<string>(),
                new List<string>(),
                new List<string>(),
                new List<string>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get move usage statistics for: {Character}", characterName);
            return Result<MoveUsageStatistics>.Failure(
                $"Move usage analysis failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<OverpoweredDetectionResult>> DetectOverpoweredElementsAsync(
        string? characterName = null,
        double? threshold = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Detecting overpowered elements");

            var thresholdValue = threshold ?? 60.0;
            var charactersToAnalyze = new List<MugenCharacter>();

            if (characterName != null)
            {
                var charResult = await _characterRepository.GetByNameAsync(characterName, ct);
                if (charResult.IsSuccess)
                    charactersToAnalyze.Add(charResult.Value);
            }
            else
            {
                charactersToAnalyze = (await _characterRepository.GetAllAsync(ct)).ToList();
            }

            var overpoweredCharacters = new List<OverpoweredCharacter>();
            var overpoweredMoves = new List<OverpoweredMove>();
            var overpoweredStrategies = new List<OverpoweredStrategy>();

            foreach (var character in charactersToAnalyze)
            {
                var winRates = await AnalyzeCharacterWinRatesAsync(character.Name, null, null, ct);
                if (winRates.IsFailure) continue;

                if (winRates.Value.OverallWinRate > thresholdValue)
                {
                    var problematicMatchups = winRates.Value.MatchupWinRates
                        .Where(m => m.WinRate > 70)
                        .Select(m => m.OpponentName)
                        .ToList();

                    overpoweredCharacters.Add(new OverpoweredCharacter(
                        character.Name,
                        winRates.Value.OverallWinRate,
                        winRates.Value.OverallWinRate - 50,
                        "Excessive win rate across all matchups",
                        problematicMatchups));
                }
            }

            return Result<OverpoweredDetectionResult>.Success(new OverpoweredDetectionResult(
                _timeProvider.UtcNow,
                thresholdValue,
                overpoweredCharacters,
                overpoweredMoves,
                overpoweredStrategies));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect overpowered elements");
            return Result<OverpoweredDetectionResult>.Failure(
                $"Detection failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<BalancePatchSuggestions>> GenerateBalanceSuggestionsAsync(
        BalanceAnalysisRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating balance suggestions");

            var characterSuggestions = new List<CharacterBalanceSuggestion>();
            var moveSuggestions = new List<MoveBalanceSuggestion>();
            var generalSuggestions = new List<GeneralBalanceSuggestion>();

            var opResult = await DetectOverpoweredElementsAsync(null, request.WinRateThreshold, ct);
            if (opResult.IsSuccess && request.SuggestNerfs)
            {
                foreach (var opChar in opResult.Value.OverpoweredCharacters)
                {
                    if (request.TargetCharacters == null || request.TargetCharacters.Contains(opChar.CharacterName))
                    {
                        characterSuggestions.Add(new CharacterBalanceSuggestion(
                            opChar.CharacterName,
                            BalanceChangeType.Nerf,
                            $"Win rate of {opChar.WinRate:F1}% exceeds threshold",
                            opChar.WinRate,
                            50.0,
                            new List<string> { "Reduce damage output", "Increase recovery frames" }));
                    }
                }
            }

            if (request.SuggestBuffs)
            {
                var characters = request.TargetCharacters ?? (await _characterRepository.GetAllAsync(ct)).Select(c => c.Name).ToList();
                foreach (var characterName in characters)
                {
                    var winRates = await AnalyzeCharacterWinRatesAsync(characterName, null, null, ct);
                    if (winRates.IsSuccess && winRates.Value.OverallWinRate < 45)
                    {
                        characterSuggestions.Add(new CharacterBalanceSuggestion(
                            characterName,
                            BalanceChangeType.Buff,
                            $"Win rate of {winRates.Value.OverallWinRate:F1}% below acceptable threshold",
                            winRates.Value.OverallWinRate,
                            50.0,
                            new List<string> { "Increase damage output", "Improve frame data" }));
                    }
                }
            }

            return Result<BalancePatchSuggestions>.Success(new BalancePatchSuggestions(
                _timeProvider.UtcNow,
                request.TargetCharacters?.Count ?? characterSuggestions.Count,
                characterSuggestions,
                moveSuggestions,
                generalSuggestions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate balance suggestions");
            return Result<BalancePatchSuggestions>.Failure(
                $"Suggestion generation failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<DetailedMatchupAnalysis>> CompareCharactersAsync(
        string character1,
        string character2,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Comparing characters: {Char1} vs {Char2}", character1, character2);

            var char1Result = await _characterRepository.GetByNameAsync(character1, ct);
            var char2Result = await _characterRepository.GetByNameAsync(character2, ct);

            if (char1Result.IsFailure)
                return Result<DetailedMatchupAnalysis>.Failure($"Character not found: {character1}", ErrorType.NotFound);
            if (char2Result.IsFailure)
                return Result<DetailedMatchupAnalysis>.Failure($"Character not found: {character2}", ErrorType.NotFound);

            var matchupStats = await _matchHistoryRepository.GetMatchupStatsAsync(
                char1Result.Value.Id, 
                char2Result.Value.Id, 
                ct);

            int char1Wins = 0, char2Wins = 0, totalMatches = 0;
            if (matchupStats.IsSuccess)
            {
                char1Wins = matchupStats.Value.Character1Wins;
                char2Wins = matchupStats.Value.Character2Wins;
                totalMatches = matchupStats.Value.TotalMatches;
            }

            var char1WinRate = totalMatches > 0 ? (double)char1Wins / totalMatches * 100 : 50;

            return Result<DetailedMatchupAnalysis>.Success(new DetailedMatchupAnalysis(
                character1,
                character2,
                totalMatches,
                char1Wins,
                char2Wins,
                char1WinRate,
                ClassifyAdvantage(char1WinRate),
                new List<MatchupFactor>(),
                new List<string>(),
                new List<string>(),
                new List<string>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare characters: {Char1} vs {Char2}", character1, character2);
            return Result<DetailedMatchupAnalysis>.Failure(
                $"Character comparison failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<CharacterRankings>> GetCharacterRankingsAsync(
        RankingCriteria criteria,
        int? topN = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting character rankings by: {Criteria}", criteria);

            var characters = await _characterRepository.GetAllAsync(ct);
            var rankings = new List<CharacterRanking>();

            foreach (var character in characters)
            {
                double score = criteria switch
                {
                    RankingCriteria.WinRate => await GetWinRateScoreAsync(character.Name, ct),
                    _ => 0
                };

                rankings.Add(new CharacterRanking(
                    0,
                    character.Name,
                    score,
                    score,
                    0,
                    new List<string>()));
            }

            rankings = rankings.OrderByDescending(r => r.Score).ToList();
            var finalRankings = rankings.Select((r, i) => r with { Rank = i + 1 }).ToList();

            if (topN.HasValue)
                finalRankings = finalRankings.Take(topN.Value).ToList();

            return Result<CharacterRankings>.Success(new CharacterRankings(
                criteria,
                _timeProvider.UtcNow,
                finalRankings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character rankings");
            return Result<CharacterRankings>.Failure(
                $"Rankings retrieval failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<PopularityTrends>> AnalyzePopularityTrendsAsync(
        TimeSpan period,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing popularity trends for period: {Days} days", period.TotalDays);

            var endDate = _timeProvider.UtcNow;
            var startDate = endDate - period;
            var characters = await _characterRepository.GetAllAsync(ct);

            var popularities = new List<CharacterPopularity>();

            foreach (var character in characters)
            {
                var matches = await _matchHistoryRepository.GetByCharacterAsync(character.Id, limit: 1000, ct);
                var periodMatches = matches.Where(m => m.PlayedAt >= startDate && m.PlayedAt <= endDate).ToList();
                
                var uniquePlayers = periodMatches.Count > 0 ? 2 : 0; // Simplified
                var pickRate = matches.Count > 0 ? (double)periodMatches.Count / matches.Count * 100 : 0;

                popularities.Add(new CharacterPopularity(
                    character.Name,
                    periodMatches.Count,
                    pickRate,
                    0,
                    SaveState.Core.Mugen.Services.TrendDirection.Stable,
                    uniquePlayers));
            }

            return Result<PopularityTrends>.Success(new PopularityTrends(
                period,
                _timeProvider.UtcNow,
                popularities.Sum(p => p.MatchesPlayed),
                popularities.OrderByDescending(p => p.PickRate).ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze popularity trends");
            return Result<PopularityTrends>.Failure(
                $"Popularity analysis failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> ExportAnalysisAsync(
        string analysisId,
        BalanceExportFormat format,
        string outputPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Exporting analysis {AnalysisId} to {Format}", analysisId, format);

            var content = format switch
            {
                BalanceExportFormat.Markdown => $"# Balance Analysis\n\nGenerated: {_timeProvider.UtcNow}\n\nAnalysis ID: {analysisId}",
                BalanceExportFormat.Json => $"{{\"analysisId\": \"{analysisId}\", \"generatedAt\": \"{_timeProvider.UtcNow:O}\"}}",
                _ => "Export not yet implemented for this format"
            };

            await File.WriteAllTextAsync(outputPath, content, ct);
            return Result<string>.Success(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export analysis");
            return Result<string>.Failure(
                $"Export failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    #region Private Helper Methods

    private async Task<List<MugenCharacter>> GetCharactersAsync(IReadOnlyList<string>? characterNames, CancellationToken ct)
    {
        if (characterNames == null || characterNames.Count == 0)
        {
            return (await _characterRepository.GetAllAsync(ct)).ToList();
        }

        var characters = new List<MugenCharacter>();
        foreach (var name in characterNames)
        {
            var result = await _characterRepository.GetByNameAsync(name, ct);
            if (result.IsSuccess)
                characters.Add(result.Value);
        }
        return characters;
    }

    private double CalculateTierScore(CharacterWinRates winRates, TierListCriteria criteria)
    {
        return criteria switch
        {
            TierListCriteria.WinRate => winRates.OverallWinRate,
            TierListCriteria.MatchupSpread => winRates.MatchupWinRates.Count > 0 
                ? winRates.MatchupWinRates.Average(m => m.WinRate) 
                : 50,
            _ => winRates.OverallWinRate
        };
    }

    private List<Tier> AssignTiers(List<CharacterTierPlacement> placements)
    {
        var tiers = new List<Tier>
        {
            new Tier("S", "Top tier - Dominant characters", 1, 5, new List<string>()),
            new Tier("A", "High tier - Strong characters", 6, 15, new List<string>()),
            new Tier("B", "Mid tier - Balanced characters", 16, 30, new List<string>()),
            new Tier("C", "Low tier - Struggling characters", 31, 45, new List<string>()),
            new Tier("D", "Bottom tier - Weak characters", 46, int.MaxValue, new List<string>())
        };

        int rank = 1;
        foreach (var placement in placements)
        {
            var tier = tiers.FirstOrDefault(t => rank >= t.MinRank && rank <= t.MaxRank);
            if (tier != null)
            {
                var charList = tier.CharacterNames.ToList();
                charList.Add(placement.CharacterName);
                tier = tier with { CharacterNames = charList };
            }
            rank++;
        }

        return tiers;
    }

    private AdvantageLevel ClassifyAdvantage(double winRate)
    {
        return winRate switch
        {
            < 40 => AdvantageLevel.HeavilyDisadvantaged,
            < 45 => AdvantageLevel.SlightlyDisadvantaged,
            < 55 => AdvantageLevel.Even,
            < 60 => AdvantageLevel.SlightlyAdvantaged,
            _ => AdvantageLevel.HeavilyAdvantaged
        };
    }

    private async Task<double> GetWinRateScoreAsync(string characterName, CancellationToken ct)
    {
        var winRates = await AnalyzeCharacterWinRatesAsync(characterName, null, null, ct);
        return winRates.IsSuccess ? winRates.Value.OverallWinRate : 0;
    }

    #endregion
}
