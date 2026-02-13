using SaveState.Core.Mugen.Entities;

namespace SaveState.Application.Mugen.DTOs;

public sealed record MugenMatchSummary(
    string Player1Name,
    string Player2Name,
    MatchResult Result,
    TimeSpan Duration,
    DateTime PlayedAt);

public sealed record MugenTierEntry(
    Guid CharacterId,
    string Name,
    int Wins,
    int Losses,
    double WinRate,
    string Tier)
{
    public int Matches => Wins + Losses;
}

public sealed record SimulatedMatchSummary(
    string RoundName,
    string Player1Name,
    string Player2Name,
    string WinnerName,
    float Confidence,
    int SimulatedPlayer1Wins,
    int SimulatedPlayer2Wins);

public sealed record BetRecord(
    Guid CharacterId,
    string CharacterName,
    int Amount,
    bool Won,
    int CreditsAfter,
    DateTime PlacedAt)
{
    public string ResultLabel => Won ? "Won" : "Lost";
}

public sealed record BetLeaderboardEntry(
    Guid CharacterId,
    string CharacterName,
    int Bets,
    int Wins,
    int Losses)
{
    public double WinRate => Bets == 0 ? 0 : (double)Wins / Bets;
}
