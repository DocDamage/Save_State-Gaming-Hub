namespace SaveState.Core.Mugen.ValueObjects;

using SaveState.Core.Mugen.Entities;

/// <summary>
/// Represents a tournament bracket containing rounds and matches.
/// </summary>
public sealed record TournamentBracket(
    Guid TournamentId,
    IReadOnlyList<TournamentRound> Rounds);

/// <summary>
/// Represents a round in a tournament bracket.
/// </summary>
public sealed record TournamentRound(
    int RoundNumber,
    string RoundName,  // "Quarter-Finals", "Semi-Finals", etc.
    IReadOnlyList<TournamentMatch> Matches);

/// <summary>
/// Represents a match in a tournament bracket.
/// </summary>
public sealed record TournamentMatch(
    Guid Id,
    Guid? Participant1Id,
    Guid? Participant2Id,
    MatchResult? Result,
    bool IsComplete);

/// <summary>
/// Represents tournament standings for tracking progress.
/// </summary>
public sealed record TournamentStanding(
    Guid ParticipantId,
    string ParticipantName,
    int Wins,
    int Losses,
    int Points,
    int CurrentRound,
    bool IsEliminated);

/// <summary>
/// Represents a path through a tournament bracket.
/// </summary>
public sealed record TournamentPath(
    IReadOnlyList<Guid> WinnerSequence,
    float Probability,
    string Description);  // "Ryu defeats Ken, then Akuma, finals vs Chun-Li"