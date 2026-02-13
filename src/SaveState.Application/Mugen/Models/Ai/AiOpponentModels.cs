namespace SaveState.Application.Mugen.Models.Ai;

/// <summary>
/// AI opponent configuration and state.
/// </summary>
public class AiOpponent
{
    public string OpponentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public string Personality { get; set; } = string.Empty;
    public NeuralNetwork? NeuralNetwork { get; set; }
    public BehaviorModel? BehaviorModel { get; set; }
    public double LearningRate { get; set; }
    public double AdaptationSpeed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastTrained { get; set; }
    public int TrainingSessions { get; set; }
    public double WinRate { get; set; }
    public TimeSpan AverageMatchLength { get; set; }
    public List<string> PreferredMoves { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public AiStatus Status { get; set; }
}

/// <summary>
/// AI opponent creation request.
/// </summary>
public class AiOpponentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int BaseDifficulty { get; set; }
    public string Personality { get; set; } = string.Empty;
    public NetworkConfig? NetworkConfig { get; set; }
    public double LearningRate { get; set; } = 0.01;
    public double AdaptationSpeed { get; set; } = 0.5;
}

/// <summary>
/// AI opponent status.
/// </summary>
public enum AiStatus
{
    Active,
    Training,
    Inactive,
    Archived
}

/// <summary>
/// Neural network configuration.
/// </summary>
public class NetworkConfig
{
    public int InputNodes { get; set; }
    public int HiddenLayers { get; set; }
    public int HiddenNodesPerLayer { get; set; }
    public int OutputNodes { get; set; }
    public string ActivationFunction { get; set; } = "ReLU";
}

/// <summary>
/// Neural network data.
/// </summary>
public class NeuralNetwork
{
    public string NetworkId { get; set; } = string.Empty;
    public NetworkConfig Config { get; set; } = new();
    public List<float> Weights { get; set; } = new();
    public List<float> Biases { get; set; } = new();
    public double Fitness { get; set; }
}

/// <summary>
/// Behavior model for AI opponents.
/// </summary>
public class BehaviorModel
{
    public string ModelId { get; set; } = string.Empty;
    public string Personality { get; set; } = string.Empty;
    public Dictionary<string, float> BehaviorWeights { get; set; } = new();
    public List<BehaviorRule> Rules { get; set; } = new();
}

/// <summary>
/// Behavior modifiers for decision making.
/// </summary>
public class BehaviorModifiers
{
    public float AggressionModifier { get; set; }
    public float DefensiveModifier { get; set; }
    public float ComboPreference { get; set; }
    public float CounterAttackChance { get; set; }
}

/// <summary>
/// Individual behavior rule.
/// </summary>
public class BehaviorRule
{
    public string RuleId { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public float Weight { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

/// <summary>
/// AI decision output.
/// </summary>
public class AiDecision
{
    public string DecisionId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public List<string> AlternativeActions { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
    public float ReactionTime { get; set; }
}

/// <summary>
/// Outcome of an AI decision.
/// </summary>
public class DecisionOutcome
{
    public string DecisionId { get; set; } = string.Empty;
    public bool WasSuccessful { get; set; }
    public float Reward { get; set; }
    public string Feedback { get; set; } = string.Empty;
}

/// <summary>
/// Game state for AI decision making.
/// </summary>
public class GameState
{
    public string StateId { get; set; } = string.Empty;
    public float PlayerHealth { get; set; }
    public float OpponentHealth { get; set; }
    public float DistanceToOpponent { get; set; }
    public string CurrentMove { get; set; } = string.Empty;
    public List<string> RecentMoves { get; set; } = new();
    public float TimeRemaining { get; set; }
    public int RoundNumber { get; set; }
}

/// <summary>
/// Analysis of game state.
/// </summary>
public class GameStateAnalysis
{
    public string AnalysisId { get; set; } = string.Empty;
    public float ThreatLevel { get; set; }
    public float OpportunityScore { get; set; }
    public string RecommendedStrategy { get; set; } = string.Empty;
    public List<string> RiskyMoves { get; set; } = new();
    public List<string> SafeMoves { get; set; } = new();
}

/// <summary>
/// Detected player patterns.
/// </summary>
public class PlayerPatterns
{
    public string PatternId { get; set; } = string.Empty;
    public List<string> CommonOpeners { get; set; } = new();
    public List<string> FavoriteCombos { get; set; } = new();
    public float AverageReactionTime { get; set; }
    public Dictionary<string, float> MoveFrequencies { get; set; } = new();
}

/// <summary>
/// Training data for AI learning.
/// </summary>
public class TrainingData
{
    public string DataId { get; set; } = string.Empty;
    public GameState State { get; set; } = new();
    public string Action { get; set; } = string.Empty;
    public float Reward { get; set; }
    public bool WasSuccessful { get; set; }
}

/// <summary>
/// Result of an AI match.
/// </summary>
public class AiMatchResult
{
    public string MatchId { get; set; } = string.Empty;
    public string OpponentId { get; set; } = string.Empty;
    public bool AiWon { get; set; }
    public TimeSpan MatchDuration { get; set; }
    public int RoundsWon { get; set; }
    public int RoundsLost { get; set; }
}

/// <summary>
/// Player action record.
/// </summary>
public class PlayerAction
{
    public string ActionId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public float Timestamp { get; set; }
    public bool WasSuccessful { get; set; }
}

/// <summary>
/// Query for AI opponents.
/// </summary>
public class AiOpponentQuery
{
    public string? NameFilter { get; set; }
    public int? MinDifficulty { get; set; }
    public int? MaxDifficulty { get; set; }
    public string? PersonalityFilter { get; set; }
    public AiStatus? StatusFilter { get; set; }
}

/// <summary>
/// AI opponent statistics.
/// </summary>
public class AiOpponentStats
{
    public string OpponentId { get; set; } = string.Empty;
    public int TotalMatches { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    public TimeSpan AverageMatchDuration { get; set; }
    public List<string> TopMoves { get; set; } = new();
    public DateTime LastPlayed { get; set; }
}

/// <summary>
/// Request to adapt AI difficulty.
/// </summary>
public class AdaptationRequest
{
    public string OpponentId { get; set; } = string.Empty;
    public double TargetWinRate { get; set; }
    public int SampleSize { get; set; }
}

/// <summary>
/// Player performance metrics.
/// </summary>
public class PlayerPerformance
{
    public string PerformanceId { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public double AverageScore { get; set; }
    public float ImprovementRate { get; set; }
    public List<string> Weaknesses { get; set; } = new();
}

/// <summary>
/// Difficulty adjustment calculation.
/// </summary>
public class DifficultyAdjustment
{
    public int CurrentDifficulty { get; set; }
    public int RecommendedDifficulty { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public float Confidence { get; set; }
}

/// <summary>
/// AI learning session.
/// </summary>
public class AiLearningSession
{
    public string SessionId { get; set; } = string.Empty;
    public string OpponentId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public List<TrainingData> TrainingData { get; set; } = new();
    public LearningProgress Progress { get; set; } = new();
}

/// <summary>
/// Learning session request.
/// </summary>
public class LearningSessionRequest
{
    public string OpponentId { get; set; } = string.Empty;
    public int TargetMatches { get; set; }
    public List<string>? FocusAreas { get; set; }
}

/// <summary>
/// Match data for learning.
/// </summary>
public class MatchData
{
    public string MatchId { get; set; } = string.Empty;
    public List<GameState> States { get; set; } = new();
    public List<AiDecision> Decisions { get; set; } = new();
    public bool AiWon { get; set; }
}

/// <summary>
/// Learning session status.
/// </summary>
public class LearningSessionStatus
{
    public string SessionId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int MatchesCompleted { get; set; }
    public int MatchesRemaining { get; set; }
    public float ProgressPercentage { get; set; }
    public string CurrentActivity { get; set; } = string.Empty;
}

/// <summary>
/// Learning progress metrics.
/// </summary>
public class LearningProgress
{
    public float AccuracyImprovement { get; set; }
    public float WinRateImprovement { get; set; }
    public int NewPatternsLearned { get; set; }
    public float AdaptationScore { get; set; }
}

/// <summary>
/// Result of AI training.
/// </summary>
public class TrainingResult
{
    public bool Success { get; set; }
    public int EpochsCompleted { get; set; }
    public float FinalAccuracy { get; set; }
    public string ModelPath { get; set; } = string.Empty;
}
