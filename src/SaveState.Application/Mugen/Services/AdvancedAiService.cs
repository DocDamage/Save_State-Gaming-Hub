using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Mugen;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Linq;
using SaveState.Core.Mugen.DTOs;
using StagePosition = SaveState.Core.Mugen.ValueObjects.Position;
using StageAreaSize = SaveState.Core.Mugen.ValueObjects.Size;
using StageElementProps = SaveState.Core.Mugen.Services.ElementProperties;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Advanced AI service providing machine learning-based opponent analysis,
/// predictive modeling, and adaptive AI opponents for MUGEN.
/// </summary>
public class AdvancedAiService : IMachineLearningService, IMatchPredictionEngine
{
    private readonly ILogger<AdvancedAiService> _logger;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, AdvancedAiServiceAiCharacterMatchupData> _matchupDatabase = new();
    private readonly Dictionary<string, AdvancedAiServicePlayerSkillModel> _playerSkillModels = new();
    private readonly NeuralNetwork _predictionModel;
    private readonly NeuralNetworkAdaptiveAiEngine _adaptiveEngine;

    public AdvancedAiService(
        ILogger<AdvancedAiService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;
        _predictionModel = new NeuralNetwork(loggerFactory.CreateLogger<NeuralNetwork>());
        _adaptiveEngine = new NeuralNetworkAdaptiveAiEngine(loggerFactory.CreateLogger<NeuralNetworkAdaptiveAiEngine>(), _timeProvider);

        InitializeMatchupDatabase();
    }

    #region IMachineLearningService Implementation

    public async Task<Result<MatchPrediction>> PredictMatchOutcomeAsync(
        string character1Name,
        string character2Name,
        PlayerSkill player1Skill,
        PlayerSkill player2Skill,
        CancellationToken ct = default)
    {
        try
        {
            // Create temporary character objects for prediction
            var c1 = MugenCharacter.Create(character1Name, "temp/char1.def", "temp/char1");
            var c2 = MugenCharacter.Create(character2Name, "temp/char2.def", "temp/char2");

            return await PredictMatchAsync(c1, c2, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting match outcome between {Char1} and {Char2}", character1Name, character2Name);
            return Result.Failure<MatchPrediction>($"Prediction failed: {ex.Message}");
        }
    }

    public Task<Result<MatchupPrediction>> AnalyzeCharacterMatchupAsync(
        string character1,
        string character2,
        CancellationToken ct = default)
    {
        var prediction = new MatchupPrediction
        {
            Advantage = MatchupAdvantage.Even,
            WinRate = 0.5,
            StrongMatchupReasons = Array.Empty<string>(),
            WeakMatchupReasons = Array.Empty<string>(),
            RecommendedStrategies = Array.Empty<string>()
        };

        _logger.LogInformation("Returning placeholder matchup prediction between {Char1} and {Char2}", character1, character2);
        return Task.FromResult(Result.Success(prediction));
    }

    public async Task<Result<CounterPickRecommendation>> GetCounterPickSuggestionsAsync(
        string opponentCharacter,
        PlayerTendencies? opponentTendencies = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating counter-pick suggestions vs {Opponent}", opponentCharacter);

            var recommendations = new List<CharacterRecommendation>();
            var strategies = new List<string>();

            var tendencies = opponentTendencies ?? new PlayerTendencies(
                new Dictionary<string, double>(),
                new Dictionary<string, double>(),
                new List<string>(),
                new List<string>(),
                new Dictionary<string, double>());

            // Analyze opponent's tendencies and suggest counters
            if (tendencies.CharacterUsage.ContainsKey("rushdown"))
            {
                recommendations.Add(new CharacterRecommendation(
                    CharacterName: "Guile",
                    MatchupScore: 0.85,
                    Advantages: new[] { "Excellent anti-air", "Projectile control", "Strong defense" },
                    Strategies: new[] { "Use flash kick for anti-airs", "Control space with sonic booms", "Punish whiffed attacks" }
                ));

                strategies.Add("Choose zoning characters to punish opponent's aggressive style");
            }

            if (tendencies.MoveFrequencies.ContainsKey("fireball") &&
                tendencies.MoveFrequencies["fireball"] > 0.3)
            {
                recommendations.Add(new CharacterRecommendation(
                    CharacterName: "Blanka",
                    MatchupScore: 0.82,
                    Advantages: new[] { "Fast movement", "Close-range power", "Anti-projectile tools" },
                    Strategies: new[] { "Roll through projectiles", "Use command grab", "Stay in close range" }
                ));

                strategies.Add("Select characters with good projectile immunity or anti-projectile moves");
            }

            var expectedWinRate = recommendations.Any() ? recommendations.Average(r => r.MatchupScore) : 0.5;

            var counterPick = new CounterPickRecommendation(
                RecommendedCharacters: recommendations,
                StrategicAdvice: strategies,
                ExpectedWinRate: expectedWinRate,
                Confidence: recommendations.Count > 2 ? ConfidenceLevel.High : ConfidenceLevel.Medium
            );

            return Result.Success<CounterPickRecommendation>(counterPick);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating counter-pick suggestions vs {Opponent}", opponentCharacter);
            return Result.Failure<CounterPickRecommendation>($"Counter-pick analysis failed: {ex.Message}");
        }
    }

    public async Task<Result<DifficultyAdjustment>> CalculateDynamicDifficultyAsync(
        PlayerPerformance performance,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Calculating dynamic difficulty adjustment");

            var recommendedDifficulty = performance.WinRate switch
            {
                > 0.8 => DifficultyLevel.Expert,
                > 0.7 => DifficultyLevel.Advanced,
                > 0.6 => DifficultyLevel.Intermediate,
                > 0.4 => DifficultyLevel.Intermediate,
                _ => DifficultyLevel.Beginner
            };

            var parameterAdjustments = new Dictionary<string, double>
            {
                ["damage_multiplier"] = performance.WinRate > 0.7 ? 1.1 : 0.9,
                ["ai_reaction_time"] = performance.SkillTrend > 0 ? 0.95 : 1.05,
                ["combo_proration"] = performance.WinRate > 0.6 ? 0.9 : 1.1,
                ["meter_gain_rate"] = performance.Strengths.Contains("Good defense") ? 1.2 : 0.8
            };

            var reasoning = $"Based on {performance.WinRate:P1} win rate and {performance.SkillTrend:+0.0;-0.0;0.0} skill trend";

            var adjustment = new DifficultyAdjustment(
                RecommendedDifficulty: recommendedDifficulty,
                ParameterAdjustments: parameterAdjustments,
                Reasoning: reasoning,
                Confidence: 0.85
            );

            return Result.Success<DifficultyAdjustment>(adjustment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dynamic difficulty");
            return Result.Failure<DifficultyAdjustment>($"Difficulty calculation failed: {ex.Message}");
        }
    }

    public async Task<Result<DifficultyAdjustment>> CalculateDynamicDifficultyAsync(
        string characterName,
        PlayerSkill skill,
        CancellationToken ct = default)
    {
        // Convert PlayerSkill to PlayerPerformance for the shared logic
        var performance = new PlayerPerformance(
            WinRate: 0.5, // Default win rate as PlayerSkill doesn't track it directly
            AverageMatchDuration: 90.0, // Default 90 seconds
            CharacterWinRates: skill.CharacterRatings,
            Strengths: new List<string>(),
            Weaknesses: new List<string>(),
            SkillTrend: 0.0
        );

        return await CalculateDynamicDifficultyAsync(performance, ct);
    }

    public async Task<Result<ProceduralMove>> GenerateProceduralMoveAsync(
        MoveGenerationParameters parameters,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating procedural move of type {Type}", parameters.MoveType);

            // Generate move properties based on parameters
            var properties = new Dictionary<string, double>
            {
                ["damage"] = parameters.PowerLevel * (parameters.Difficulty == DifficultyLevel.Advanced ? 1.2 : 1.0),
                ["startup_frames"] = 8 + (parameters.Difficulty == DifficultyLevel.Beginner ? 2 : 0),
                ["active_frames"] = parameters.MoveType == MoveType.Special ? 6 : 4,
                ["recovery_frames"] = 12 + (parameters.PowerLevel > 1.5 ? 4 : 0)
            };

            var mechanics = GenerateMechanics(parameters);
            var balanceScore = CalculateBalanceScore(properties, mechanics, parameters);

            var move = new ProceduralMove
            {
                Name = GenerateMoveName(parameters),
                Description = $"AI-generated {parameters.MoveType.ToString().ToLower()} move",
                Type = parameters.MoveType,
                Properties = properties,
                Mechanics = mechanics,
                BalanceScore = balanceScore
            };

            return Result.Success<ProceduralMove>(move);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating procedural move");
            return Result.Failure<ProceduralMove>($"Move generation failed: {ex.Message}");
        }
    }

    public async Task<Result<ProceduralStage>> GenerateProceduralStageAsync(
        StageGenerationParameters? parameters = null,
        CancellationToken ct = default)
    {
        try
        {
            var genParams = parameters ?? new StageGenerationParameters(
                "Training Room",
                DifficultyLevel.Intermediate,
                new List<string>(),
                new List<string>(),
                StageSize.Medium);

            _logger.LogInformation("Generating procedural stage with {Theme} theme", genParams.Theme);

            var elements = GenerateStageElements(genParams);
            var properties = new Dictionary<string, double>
            {
                ["width"] = genParams.Size == StageSize.Large ? 1280 : 960,
                ["height"] = genParams.Size == StageSize.Large ? 720 : 480,
                ["camera_speed"] = 1.0,
                ["scroll_speed"] = 0.8
            };

            var balanceScore = CalculateStageBalanceScore(elements, properties, genParams);

            var stage = new ProceduralStage(
                Name: GenerateStageName(genParams),
                Description: $"AI-generated {genParams.Theme} arena",
                Difficulty: genParams.Difficulty,
                Size: genParams.Size,
                Elements: elements,
                Properties: properties,
                BalanceScore: balanceScore,
                Theme: genParams.Theme);

            return Result.Success<ProceduralStage>(stage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating procedural stage");
            return Result.Failure<ProceduralStage>($"Stage generation failed: {ex.Message}");
        }
    }

    public Task<Result<MatchupAnalysis>> AnalyzeCharacterBalanceAsync(
        string characterName,
        CancellationToken ct = default)
    {
        var analysis = new MatchupAnalysis
        {
            CharacterName = characterName,
            TierRating = "A",
            BalanceScore = 55,
            Summary = "Placeholder balance analysis.",
            ActionableTips = Array.Empty<string>(),
            MoveAnalyses = Array.Empty<string>(),
            Recommendations = Array.Empty<string>()
        };

        _logger.LogInformation("Returning placeholder balance analysis for {Character}", characterName);
        return Task.FromResult(Result.Success(analysis));
    }

    public Task<Result<IReadOnlyList<TrainingModel>>> GetTrainedModelsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<IReadOnlyList<TrainingModel>>(Array.Empty<TrainingModel>()));
    }

    public Task<Result<TrainingModel>> TrainModelAsync(
        TrainingConfiguration configuration,
        IProgress<TrainingProgress> progress,
        CancellationToken cancellationToken = default)
    {
        var model = new TrainingModel
        {
            Id = Guid.NewGuid(),
            Name = configuration.ModelName,
            CharacterName = null,
            TrainedAt = _timeProvider.UtcNow,
            Accuracy = 0.0,
            SampleCount = 0,
            IsActive = false,
            Algorithm = configuration.Algorithm,
            TotalEpochs = configuration.TotalEpochs,
            ModelSize = 0
        };

        return Task.FromResult(Result.Success(model));
    }

    public Task<Result<CharacterPerformanceAnalysis>> AnalyzeCharacterPerformanceAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        var analysis = new CharacterPerformanceAnalysis
        {
            CharacterId = characterId,
            OverallStrength = 0,
            Strengths = Array.Empty<string>(),
            Weaknesses = Array.Empty<string>(),
            RecommendedImprovements = Array.Empty<string>()
        };

        return Task.FromResult(Result.Success(analysis));
    }

    public Task<Result> DeleteModelAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting model {ModelId} (placeholder)", modelId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<string>> ExportModelAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        var path = $"models/{modelId}.zip";
        _logger.LogInformation("Exporting model {ModelId} to {Path} (placeholder)", modelId, path);
        return Task.FromResult(Result.Success(path));
    }

    #endregion

    #region IMatchPredictionEngine Implementation

    public async Task<Result<MatchPrediction>> PredictMatchAsync(
        MugenCharacter character1,
        MugenCharacter character2,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Predicting match outcome: {Char1} vs {Char2}",
                character1.Name, character2.Name);

            // Use neural network for prediction
            var prediction = await _predictionModel.PredictAsync(character1, character2, ct);

            // Map application NeuralMatchPrediction -> core MatchPrediction
            var matchPrediction = new MatchPrediction(
                WinProbabilityPlayer1: (float)prediction.WinProbability,
                WinProbabilityPlayer2: (float)(1.0 - prediction.WinProbability),
                DrawProbability: 0f,
                Factors: prediction.KeyFactors.Select(k => new MatchFactor(k, 0f, 0f, 1f)).ToList(),
                Reasoning: $"Neural model prediction (confidence: {prediction.Confidence:F2})"
            );

            return Result.Success<MatchPrediction>(matchPrediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting match between {Char1} and {Char2}", character1.Name, character2.Name);
            return Result.Failure<MatchPrediction>($"Prediction failed: {ex.Message}");
        }
    }

    public async Task<Result> TrainWithResultAsync(
        MugenMatchHistory actualResult,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Training prediction model with match result");

            // Train the neural network with actual results
            await _predictionModel.TrainAsync(actualResult, ct);

            // Update player skill models
            await UpdatePlayerSkillModelsAsync(actualResult, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training with match result");
            return Result.Failure($"Training failed: {ex.Message}");
        }
    }

    #endregion

    #region Private Methods

    private void InitializeMatchupDatabase()
    {
        // Initialize with known character matchups
        _matchupDatabase["ryu_vs_ken"] = new AdvancedAiServiceAiCharacterMatchupData
        {
            WinRate = 0.52,
            Strengths = new[] { "Similar playstyles create mirror matches", "Both have strong fundamentals" },
            Weaknesses = new[] { "Predictable fireball usage", "Similar recovery times" },
            Strategies = new[] { "Focus on footsies and conditioning", "Use throws to break rhythm" }
        };

        _matchupDatabase["ryu_vs_guile"] = new AdvancedAiServiceAiCharacterMatchupData
        {
            WinRate = 0.48,
            Strengths = new[] { "Good anti-air tools", "Projectile immunity when close" },
            Weaknesses = new[] { "Susceptible to zoning", "Sonic boom covers space well" },
            Strategies = new[] { "Close distance quickly", "Use throws when possible", "Anti-air with shoryuken" }
        };

        _logger.LogInformation("Initialized matchup database with {Count} matchups", _matchupDatabase.Count);
    }

    private async Task<AdvancedAiServiceAiCharacterMatchupData> GenerateMatchupAnalysisAsync(string char1, string char2, CancellationToken ct)
    {
        // Generate analysis for unknown matchups using heuristics
        var random = new Random(char1.GetHashCode() ^ char2.GetHashCode());

        return new AdvancedAiServiceAiCharacterMatchupData
        {
            WinRate = 0.45 + random.NextDouble() * 0.1, // 45-55% win rate
            Strengths = new[] { "Generated matchup analysis", "Balanced characteristics" },
            Weaknesses = new[] { "Limited matchup data", "May require player adaptation" },
            Strategies = new[] { "Focus on fundamentals", "Adapt to opponent's patterns", "Use character-specific tools" }
        };
    }

    private MatchupAdvantage CalculateAdvantage(double winRate)
    {
        return winRate switch
        {
            > 0.65 => MatchupAdvantage.StronglyFavored,
            > 0.55 => MatchupAdvantage.SlightlyFavored,
            > 0.45 => MatchupAdvantage.Even,
            > 0.35 => MatchupAdvantage.SlightlyUnfavored,
            _ => MatchupAdvantage.StronglyUnfavored
        };
    }

    private string GetMatchupKey(string char1, string char2)
    {
        var chars = new[] { char1, char2 }.OrderBy(c => c).ToArray();
        return $"{chars[0].ToLower()}_vs_{chars[1].ToLower()}";
    }

    private string GenerateMoveName(MoveGenerationParameters parameters)
    {
        var prefixes = parameters.MoveType switch
        {
            MoveType.Normal => new[] { "Quick", "Heavy", "Light", "Medium" },
            MoveType.Special => new[] { "Energy", "Aerial", "Ground", "Charged" },
            MoveType.Super => new[] { "Ultimate", "Final", "Devastating", "Supreme" },
            _ => new[] { "Special", "Unique", "Custom", "Generated" }
        };

        var effects = new[] { "Blast", "Strike", "Wave", "Rush", "Beam", "Fist", "Kick" };
        var random = new Random();

        return $"{prefixes[random.Next(prefixes.Length)]} {effects[random.Next(effects.Length)]}";
    }

    private IReadOnlyList<string> GenerateMechanics(MoveGenerationParameters parameters)
    {
        var mechanics = new List<string>();

        if (parameters.RequiredMechanics.Contains("projectile"))
            mechanics.Add("Fires projectile");

        if (parameters.RequiredMechanics.Contains("invincible"))
            mechanics.Add("Upper body invincible on startup");

        if (parameters.MoveType == MoveType.Special)
            mechanics.Add("Costs 1 meter stock");

        if (parameters.PowerLevel > 1.2)
            mechanics.Add("High damage output");

        return mechanics;
    }

    private double CalculateBalanceScore(Dictionary<string, double> properties, IReadOnlyList<string> mechanics, MoveGenerationParameters parameters)
    {
        var score = 1.0;

        // Adjust based on damage
        if (properties["damage"] > 100) score *= 0.9;
        if (properties["damage"] < 50) score *= 1.1;

        // Adjust based on startup
        if (properties["startup_frames"] < 5) score *= 0.8;
        if (properties["startup_frames"] > 15) score *= 1.2;

        // Adjust based on difficulty
        if (parameters.Difficulty == DifficultyLevel.Beginner) score *= 1.1;
        if (parameters.Difficulty == DifficultyLevel.Advanced) score *= 0.9;

        return Math.Clamp(score, 0.1, 2.0);
    }

    private string GenerateStageName(StageGenerationParameters parameters)
    {
        var themes = new[] { "Arena", "Dojo", "Stadium", "Temple", "Lab", "City", "Mountain" };
        var suffixes = new[] { "Battleground", "Coliseum", "Showdown", "Clash", "Warzone" };

        var random = new Random();
        return $"{themes[random.Next(themes.Length)]} {suffixes[random.Next(suffixes.Length)]}";
    }

    private IReadOnlyList<StageElement> GenerateStageElements(StageGenerationParameters parameters)
    {
        var elements = new List<StageElement>();
        var defaultProperties = new StageElementProps(true, 0, null);

        // Add basic elements based on theme
        elements.Add(new StageElement(
            "Floor",
            new StagePosition(0, 0),
            new StageAreaSize(1000, 50),
            $"{parameters.Theme}_floor",
            defaultProperties));

        if (parameters.RequiredElements.Contains("walls"))
        {
            elements.Add(new StageElement(
                "Wall",
                new StagePosition(-400, 0),
                new StageAreaSize(50, 300),
                $"{parameters.Theme}_wall",
                defaultProperties));
        }

        return elements;
    }

    private double CalculateStageBalanceScore(IReadOnlyList<StageElement> elements, Dictionary<string, double> properties, StageGenerationParameters parameters)
    {
        var score = 1.0;

        // Adjust based on size
        if (parameters.Size == StageSize.Large) score *= 1.1;
        if (parameters.Size == StageSize.Small) score *= 0.9;

        // Adjust based on difficulty
        if (parameters.Difficulty == DifficultyLevel.Advanced) score *= 0.95;

        return Math.Clamp(score, 0.1, 2.0);
    }



    private async Task UpdatePlayerSkillModelsAsync(MugenMatchHistory result, CancellationToken ct)
    {
        // Update skill models based on match results
        var winnerId = result.Result == MatchResult.Player1Win ? result.Player1CharacterId : result.Player2CharacterId;
        var loserId = result.Result == MatchResult.Player1Win ? result.Player2CharacterId : result.Player1CharacterId;

        // Update Elo ratings (simplified)
        if (!_playerSkillModels.ContainsKey(winnerId.ToString()))
        {
            _playerSkillModels[winnerId.ToString()] = new AdvancedAiServicePlayerSkillModel
            {
                Rating = 1500,
                LastUpdated = _timeProvider.UtcNow
            };
        }

        if (!_playerSkillModels.ContainsKey(loserId.ToString()))
        {
            _playerSkillModels[loserId.ToString()] = new AdvancedAiServicePlayerSkillModel
            {
                Rating = 1500,
                LastUpdated = _timeProvider.UtcNow
            };
        }

        // Simple Elo update
        var winner = _playerSkillModels[winnerId.ToString()];
        var loser = _playerSkillModels[loserId.ToString()];

        var expectedWinner = 1.0 / (1.0 + Math.Pow(10, (loser.Rating - winner.Rating) / 400.0));
        var expectedLoser = 1.0 - expectedWinner;

        winner.Rating += 32 * (1.0 - expectedWinner);
        loser.Rating += 32 * (0.0 - expectedLoser);
        winner.LastUpdated = _timeProvider.UtcNow;
        loser.LastUpdated = _timeProvider.UtcNow;
    }

    #endregion
}

/// <summary>
/// Character matchup data for AI analysis.
/// </summary>
public class AdvancedAiServiceAiCharacterMatchupData
{
    public double WinRate { get; set; }
    public IReadOnlyList<string> Strengths { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Weaknesses { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Strategies { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Player skill model for rating calculations.
/// </summary>
public class AdvancedAiServicePlayerSkillModel
{
    public double Rating { get; set; }
    public double Volatility { get; set; } = 0.06;
    public IReadOnlyDictionary<string, double> CharacterRatings { get; set; } = new Dictionary<string, double>();
    public DateTime LastUpdated { get; set; } = DateTime.UnixEpoch;
}
