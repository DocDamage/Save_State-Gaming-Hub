namespace SaveState.Core.Mugen.ValueObjects;

using SaveState.Core.Mugen.Entities;

/// <summary>
/// Represents the result of a death match simulation.
/// </summary>
public sealed record SimulationResult(
    Guid Character1Id,
    string Character1Name,
    Guid Character2Id,
    string Character2Name,
    int TotalSimulations,
    int Character1Wins,
    int Character2Wins,
    int Draws,
    float Character1WinRate,
    float Character2WinRate,
    float Confidence,  // Statistical confidence in prediction
    TimeSpan SimulationDuration,
    IReadOnlyList<RoundPrediction> RoundBreakdown);

/// <summary>
/// Represents a prediction for a single round.
/// </summary>
public sealed record RoundPrediction(
    int RoundNumber,
    float Character1WinProbability,
    string PredictedWinner,
    string KeyFactor);  // "Range advantage", "Damage output", etc.

/// <summary>
/// Represents a complete tournament simulation.
/// </summary>
public sealed record TournamentSimulation(
    Guid Id,
    IReadOnlyList<Guid> Participants,
    TournamentFormat Format,
    int SimulationsPerMatch,
    IReadOnlyList<SimulatedBracket> Brackets,
    Guid PredictedWinnerId,
    string PredictedWinnerName,
    float WinnerConfidence,
    IReadOnlyList<TournamentPath> TopPaths,  // Most likely tournament outcomes
    DateTime SimulatedAt);

/// <summary>
/// Represents a simulated bracket in a tournament.
/// </summary>
public sealed record SimulatedBracket(
    int Round,
    string RoundName,
    IReadOnlyList<SimulatedMatch> Matches);

/// <summary>
/// Represents a simulated match result.
/// </summary>
public sealed record SimulatedMatch(
    Guid Participant1Id,
    Guid Participant2Id,
    Guid PredictedWinnerId,
    float WinConfidence,
    int SimulatedP1Wins,
    int SimulatedP2Wins);

/// <summary>
/// Represents factors that influence match predictions.
/// </summary>
public sealed record MatchFactor(
    string Name,          // "Damage Output", "Speed", "Range", "Tier"
    float Character1Score,
    float Character2Score,
    float Weight);

/// <summary>
/// Represents the prediction result for a single match.
/// </summary>
public sealed record MatchPrediction(
    float WinProbabilityPlayer1,
    float WinProbabilityPlayer2,
    float DrawProbability,
    IReadOnlyList<MatchFactor> Factors,
    string Reasoning);

/// <summary>
/// Represents different MUGEN engines.
/// </summary>
public enum MugenEngine
{
    IkemenGo,
    ClassicMugen,
    Custom
}
