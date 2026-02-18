using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Neural network implementation for match prediction and AI analysis.
/// Simplified implementation demonstrating machine learning concepts.
/// </summary>
public class NeuralNetwork
{
    private readonly ILogger<NeuralNetwork> _logger;
    private readonly double[] _weights;
    private readonly Random _random = new();

    public NeuralNetwork(ILogger<NeuralNetwork> logger)
    {
        _logger = logger;
        _weights = InitializeWeights(20); // 20 input features
    }

    public async Task<NeuralMatchPrediction> PredictAsync(
        MugenCharacter character1,
        MugenCharacter character2,
        CancellationToken ct = default)
    {
        try
        {
            // Extract features from characters
            var features = ExtractFeatures(character1, character2);

            // Forward pass through neural network
            var output = Sigmoid(DotProduct(features, _weights));

            // Convert to win probability
            var winProbability = output;
            var predictedWinner = winProbability > 0.5 ? character1.Name : character2.Name;

            var prediction = new NeuralMatchPrediction(
                Character1Name: character1.Name,
                Character2Name: character2.Name,
                PredictedWinner: predictedWinner,
                WinProbability: winProbability,
                Confidence: CalculateConfidence(output),
                KeyFactors: ExtractKeyFactors(character1, character2, features),
                PredictedMatchLength: TimeSpan.FromMinutes(2.5 + _random.NextDouble() * 2.0)
            );

            return prediction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in neural network prediction");
            // Return default prediction
            return new NeuralMatchPrediction(
                Character1Name: character1.Name,
                Character2Name: character2.Name,
                PredictedWinner: character1.Name,
                WinProbability: 0.5,
                Confidence: 0.5,
                KeyFactors: Array.Empty<string>(),
                PredictedMatchLength: TimeSpan.FromMinutes(3.0)
            );
        }
    }

    public async Task TrainAsync(MugenMatchHistory result, CancellationToken ct = default)
    {
        try
        {
            // Extract features from match result
            var features = ExtractFeaturesFromHistory(result);
            var actualOutcome = result.Result == MatchResult.Player1Win ? 1.0 : 0.0;

            // Simple gradient descent update
            var prediction = Sigmoid(DotProduct(features, _weights));
            var error = actualOutcome - prediction;

            // Update weights
            for (int i = 0; i < _weights.Length; i++)
            {
                _weights[i] += 0.01 * error * features[i]; // Learning rate = 0.01
            }

            _logger.LogDebug("Neural network trained with match result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training neural network");
        }
    }

    private double[] InitializeWeights(int size)
    {
        var weights = new double[size];
        for (int i = 0; i < size; i++)
        {
            weights[i] = (_random.NextDouble() - 0.5) * 0.1; // Small random weights
        }
        return weights;
    }

    private double[] ExtractFeatures(MugenCharacter char1, MugenCharacter char2)
    {
        // Extract numerical features for prediction
        return new double[]
        {
            // Basic attributes (normalized 0-1)
            char1.Health / 2000.0,      // Health
            char1.Attack / 200.0,       // Attack
            char1.Defense / 200.0,      // Defense
            char1.Speed / 10.0,         // Speed

            char2.Health / 2000.0,      // Health
            char2.Attack / 200.0,       // Attack
            char2.Defense / 200.0,      // Defense
            char2.Speed / 10.0,         // Speed

            // Character type advantages (0 or 1)
            char1.IsProjectileCharacter ? 1.0 : 0.0,
            char1.IsRushdownCharacter ? 1.0 : 0.0,
            char1.IsZoningCharacter ? 1.0 : 0.0,

            char2.IsProjectileCharacter ? 1.0 : 0.0,
            char2.IsRushdownCharacter ? 1.0 : 0.0,
            char2.IsZoningCharacter ? 1.0 : 0.0,

            // Special mechanics
            char1.HasSuperArts ? 1.0 : 0.0,
            char1.HasThrows ? 1.0 : 0.0,
            char1.HasCommandGrab ? 1.0 : 0.0,

            char2.HasSuperArts ? 1.0 : 0.0,
            char2.HasThrows ? 1.0 : 0.0,
            char2.HasCommandGrab ? 1.0 : 0.0
        };
    }

    private double[] ExtractFeaturesFromHistory(MugenMatchHistory result)
    {
        // Extract features from historical match data
        // This would be more sophisticated in a real implementation
        return new double[]
        {
            result.RoundsWonP1 / 3.0,        // Normalized rounds won
            result.RoundsWonP2 / 3.0,
            result.MatchDuration.TotalMinutes / 10.0,  // Normalized duration
            // Add more features as needed
        }.Concat(new double[16]).ToArray(); // Pad to match feature count
    }

    private double DotProduct(double[] a, double[] b)
    {
        return a.Zip(b, (x, y) => x * y).Sum();
    }

    private double Sigmoid(double x)
    {
        return 1.0 / (1.0 + Math.Exp(-x));
    }

    private double CalculateConfidence(double output)
    {
        // Confidence based on how extreme the prediction is
        var distanceFrom50 = Math.Abs(output - 0.5);
        return Math.Min(distanceFrom50 * 2, 0.95); // Max 95% confidence
    }

    private IReadOnlyList<string> ExtractKeyFactors(MugenCharacter char1, MugenCharacter char2, double[] features)
    {
        var factors = new List<string>();

        // Analyze feature differences
        var healthDiff = features[0] - features[4];
        if (Math.Abs(healthDiff) > 0.1)
        {
            factors.Add(healthDiff > 0 ? $"{char1.Name} has health advantage" : $"{char2.Name} has health advantage");
        }

        var attackDiff = features[1] - features[5];
        if (Math.Abs(attackDiff) > 0.1)
        {
            factors.Add(attackDiff > 0 ? $"{char1.Name} has attack advantage" : $"{char2.Name} has attack advantage");
        }

        // Check character type matchups
        if (features[8] > 0 && features[11] > 0) // Both projectile characters
            factors.Add("Projectile vs projectile matchup favors spacing control");

        if (features[9] > 0 && features[12] > 0) // Both rushdown characters
            factors.Add("Rushdown mirror match favors fundamentals");

        return factors.Take(3).ToList(); // Limit to top 3 factors
    }
}

/// <summary>
/// Adaptive AI engine that adjusts opponent behavior based on player performance.
/// </summary>
public class NeuralNetworkAdaptiveAiEngine
{
    private readonly ILogger<NeuralNetworkAdaptiveAiEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, NeuralNetworkAiProfile> _aiProfiles = new();

    public NeuralNetworkAdaptiveAiEngine(ILogger<NeuralNetworkAdaptiveAiEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        InitializeAiProfiles();
    }

    public async Task<NeuralNetworkAiBehaviorProfile> AdaptToPlayerAsync(
        string playerId,
        PlayerPerformance performance,
        CancellationToken ct = default)
    {
        try
        {
            var profileKey = $"player_{playerId}";
            if (!_aiProfiles.TryGetValue(profileKey, out var profile))
            {
                profile = new NeuralNetworkAiProfile { PlayerId = playerId };
                _aiProfiles[profileKey] = profile;
            }

            // Adapt AI behavior based on player performance
            var behavior = new NeuralNetworkAiBehaviorProfile
            {
                Difficulty = CalculateAdaptiveDifficulty(performance),
                Aggressiveness = CalculateAggressiveness(performance),
                DefenseStyle = SelectDefenseStyle(performance),
                OffensivePatterns = SelectOffensivePatterns(performance),
                AdaptationReasoning = GenerateAdaptationReasoning(performance)
            };

            // Update profile with learning
            UpdateAiProfile(profile, performance);

            return behavior;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adapting AI to player {PlayerId}", playerId);
            return GetDefaultBehavior();
        }
    }

    public async Task<NeuralNetworkAiCounterStrategy> GenerateCounterStrategyAsync(
        string playerMove,
        string playerCharacter,
        CancellationToken ct = default)
    {
        try
        {
            // Generate AI response to specific player moves
            var strategy = new NeuralNetworkAiCounterStrategy
            {
                PlayerMove = playerMove,
                CounterMoves = GetCounterMoves(playerMove, playerCharacter),
                Timing = CalculateOptimalTiming(playerMove),
                Positioning = CalculateOptimalPositioning(playerMove),
                SuccessProbability = CalculateCounterSuccess(playerMove)
            };

            return strategy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating counter strategy for {Move}", playerMove);
            return new NeuralNetworkAiCounterStrategy
            {
                PlayerMove = playerMove,
                CounterMoves = new[] { "Block", "Evade" },
                Timing = "Immediate",
                Positioning = "Neutral",
                SuccessProbability = 0.5
            };
        }
    }

    private void InitializeAiProfiles()
    {
        // Initialize with some base AI profiles
        _aiProfiles["default"] = new NeuralNetworkAiProfile
        {
            Aggressiveness = 0.5,
            DefenseRating = 0.5,
            AdaptationCount = 0
        };
    }

    private DifficultyLevel CalculateAdaptiveDifficulty(PlayerPerformance performance)
    {
        return performance.WinRate switch
        {
            > 0.8 => DifficultyLevel.VeryHard,
            > 0.7 => DifficultyLevel.Hard,
            > 0.6 => DifficultyLevel.Medium,
            > 0.4 => DifficultyLevel.Easy,
            _ => DifficultyLevel.VeryEasy
        };
    }

    private double CalculateAggressiveness(PlayerPerformance performance)
    {
        // More aggressive if player has defensive weaknesses
        var defensiveWeaknesses = performance.Weaknesses.Count(w =>
            w.Contains("defense", StringComparison.OrdinalIgnoreCase) ||
            w.Contains("blocking", StringComparison.OrdinalIgnoreCase));

        return Math.Min(0.8, 0.4 + (defensiveWeaknesses * 0.1));
    }

    private string SelectDefenseStyle(PlayerPerformance performance)
    {
        if (performance.Weaknesses.Contains("High damage received"))
            return "AggressiveCounter";

        if (performance.Strengths.Contains("Good defense"))
            return "PatientDefense";

        return "BalancedDefense";
    }

    private IReadOnlyList<string> SelectOffensivePatterns(PlayerPerformance performance)
    {
        var patterns = new List<string> { "Fundamentals", "Mix-ups" };

        if (performance.Strengths.Contains("Good combos"))
            patterns.Add("ComboExtensions");

        if (performance.Weaknesses.Contains("Anti-air defense"))
            patterns.Add("AntiAirPressure");

        return patterns;
    }

    private string GenerateAdaptationReasoning(PlayerPerformance performance)
    {
        var reasons = new List<string>();

        if (performance.WinRate > 0.7)
            reasons.Add("Player showing strong performance, increasing challenge");

        if (performance.Weaknesses.Any())
            reasons.Add($"Exploiting weaknesses: {string.Join(", ", performance.Weaknesses.Take(2))}");

        if (performance.SkillTrend > 0)
            reasons.Add("Player improving, adapting difficulty upward");

        return reasons.Any() ? string.Join("; ", reasons) : "Balanced adaptation based on performance";
    }

    private NeuralNetworkAiBehaviorProfile GetDefaultBehavior()
    {
        return new NeuralNetworkAiBehaviorProfile
        {
            Difficulty = DifficultyLevel.Medium,
            Aggressiveness = 0.5,
            DefenseStyle = "BalancedDefense",
            OffensivePatterns = new[] { "Fundamentals", "Mix-ups" },
            AdaptationReasoning = "Default AI behavior"
        };
    }

    private void UpdateAiProfile(NeuralNetworkAiProfile profile, PlayerPerformance performance)
    {
        // Update profile based on recent performance
        profile.AdaptationCount++;
        profile.LastPerformance = performance;
        profile.LastUpdate = _timeProvider.UtcNow;

        // Adjust base characteristics
        if (performance.WinRate > 0.6)
        {
            profile.Aggressiveness = Math.Min(0.9, profile.Aggressiveness + 0.05);
        }
        else
        {
            profile.Aggressiveness = Math.Max(0.1, profile.Aggressiveness - 0.05);
        }
    }

    private IReadOnlyList<string> GetCounterMoves(string playerMove, string playerCharacter)
    {
        // Simplified counter move selection
        return playerMove.ToLower() switch
        {
            var m when m.Contains("fireball") => new[] { "Jump over", "Reflect", "Close distance" },
            var m when m.Contains("punch") => new[] { "Block", "Counter punch", "Throw" },
            var m when m.Contains("kick") => new[] { "Block", "Sweep", "Anti-air" },
            var m when m.Contains("throw") => new[] { "Tech throw", "Evade", "Punish" },
            _ => new[] { "Block", "Evade", "Counter" }
        };
    }

    private string CalculateOptimalTiming(string playerMove)
    {
        return playerMove.ToLower() switch
        {
            var m when m.Contains("super") => "During super flash",
            var m when m.Contains("special") => "During recovery",
            var m when m.Contains("normal") => "During active frames",
            _ => "Immediate"
        };
    }

    private string CalculateOptimalPositioning(string playerMove)
    {
        return playerMove.ToLower() switch
        {
            var m when m.Contains("fireball") => "Close range",
            var m when m.Contains("uppercut") => "Cornered",
            var m when m.Contains("sweep") => "Mid-range",
            _ => "Neutral"
        };
    }

    private double CalculateCounterSuccess(string playerMove)
    {
        // Simplified success probability
        return playerMove.ToLower() switch
        {
            var m when m.Contains("super") => 0.3,  // Supers are hard to punish
            var m when m.Contains("special") => 0.7, // Specials have recovery
            var m when m.Contains("normal") => 0.6,  // Normals vary
            _ => 0.5
        };
    }
}

/// <summary>
/// AI behavior profile for adaptive opponents.
/// </summary>
public class NeuralNetworkAiBehaviorProfile
{
    public DifficultyLevel Difficulty { get; set; } = default!;
    public double Aggressiveness { get; set; } = default!;
    public string DefenseStyle { get; set; } = default!;
    public IReadOnlyList<string> OffensivePatterns { get; set; } = default!;
    public string AdaptationReasoning { get; set; } = default!;
}

/// <summary>
/// AI counter strategy for specific moves.
/// </summary>
public class NeuralNetworkAiCounterStrategy
{
    public string PlayerMove { get; set; } = default!;
    public IReadOnlyList<string> CounterMoves { get; set; } = default!;
    public string Timing { get; set; } = default!;
    public string Positioning { get; set; } = default!;
    public double SuccessProbability { get; set; } = default!;
}

/// <summary>
/// AI profile for learning player patterns.
/// </summary>
public class NeuralNetworkAiProfile
{
    public string PlayerId { get; set; } = string.Empty;
    public double Aggressiveness { get; set; } = 0.5;
    public double DefenseRating { get; set; } = 0.5;
    public int AdaptationCount { get; set; }
    public PlayerPerformance? LastPerformance { get; set; }
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}

public record NeuralMatchPrediction(
    string Character1Name,
    string Character2Name,
    string PredictedWinner,
    double WinProbability,
    double Confidence,
    IReadOnlyList<string> KeyFactors,
    TimeSpan PredictedMatchLength);
