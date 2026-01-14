using System;
using System.Collections.Generic;
using SaveState.Core.Mugen.Entities;

namespace SaveState.Core.Mugen.ValueObjects;

public enum MatchupAdvantage
{
    StronglyFavored,
    SlightlyFavored,
    Even,
    SlightlyUnfavored,
    StronglyUnfavored
}

public sealed class MatchupPrediction
{
    public MatchupPrediction()
    {
        StrongMatchupReasons = Array.Empty<string>();
        WeakMatchupReasons = Array.Empty<string>();
        RecommendedStrategies = Array.Empty<string>();
    }

    public MatchupAdvantage Advantage { get; set; }
    public double WinRate { get; set; }
    public IReadOnlyList<string> StrongMatchupReasons { get; set; }
    public IReadOnlyList<string> WeakMatchupReasons { get; set; }
    public IReadOnlyList<string> RecommendedStrategies { get; set; }
}

public sealed class MatchupAnalysis
{
    public string TierRating { get; set; } = "C";
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<string> ActionableTips { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> MoveAnalyses { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; set; } = Array.Empty<string>();
    public string CharacterName { get; set; } = string.Empty;
    public double BalanceScore { get; set; }
    public double PredictedWinRate { get; set; }
}

public sealed class ProceduralMove
{
    public string Name { get; set; } = string.Empty;
    public MoveType Type { get; set; }
    public double BalanceScore { get; set; }
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<string> Mechanics { get; set; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, double> Properties { get; set; } = new Dictionary<string, double>();
}

public record MoveGenerationParameters(
    MoveType MoveType,
    DifficultyLevel Difficulty,
    IReadOnlyList<string> RequiredMechanics,
    IReadOnlyList<string> AvoidedMechanics,
    double PowerLevel,
    string Theme);

/// <summary>
/// Training configuration for machine learning models.
/// </summary>
public sealed record TrainingConfiguration(
    string ModelName,
    string Algorithm,
    int TotalEpochs,
    double LearningRate,
    int BatchSize,
    bool useGpu
);

/// <summary>
/// Training progress information.
/// </summary>
public sealed class TrainingProgress
{
    public int CurrentEpoch { get; set; }
    public int TotalEpochs { get; set; }
    public double Loss { get; set; }
    public double Accuracy { get; set; }
    public double ValidationLoss { get; set; }
    public double ValidationAccuracy { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Character performance analysis result.
/// </summary>
public sealed class CharacterPerformanceAnalysis
{
    public Guid CharacterId { get; set; }
    public double OverallStrength { get; set; }
    public string[] Strengths { get; set; } = Array.Empty<string>();
    public string[] Weaknesses { get; set; } = Array.Empty<string>();
    public string[] RecommendedImprovements { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Move data for creation/editing.
/// </summary>
public sealed record MoveData(
    string Name,
    string Command,
    string Type,
    int Damage,
    int Startup,
    int Active,
    int Recovery,
    int BlockAdvantage,
    int HitAdvantage,
    string Properties,
    string AnimationFrames,
    string HitboxData,
    string SoundEffects,
    string Notes
);
