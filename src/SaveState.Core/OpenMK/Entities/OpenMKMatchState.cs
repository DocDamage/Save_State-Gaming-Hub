using System;
using SaveState.Core.OpenMK.Services;

namespace SaveState.Core.OpenMK.Entities;

/// <summary>
/// Represents the persisted state of an OpenMK match.
/// </summary>
public class OpenMKMatchState
{
    private OpenMKMatchState() { }

    public OpenMKMatchState(
        Guid matchId,
        Guid player1CharacterId,
        Guid player2CharacterId,
        int roundNumber,
        int player1Health,
        int player2Health,
        int player1SuperBar,
        int player2SuperBar,
        int player1Wins,
        int player2Wins,
        TimeSpan roundTimeRemaining,
        OpenMKMatchPhase phase)
    {
        MatchId = matchId;
        Player1CharacterId = player1CharacterId;
        Player2CharacterId = player2CharacterId;
        RoundNumber = roundNumber;
        Player1Health = player1Health;
        Player2Health = player2Health;
        Player1SuperBar = player1SuperBar;
        Player2SuperBar = player2SuperBar;
        Player1Wins = player1Wins;
        Player2Wins = player2Wins;
        RoundTimeRemaining = roundTimeRemaining;
        Phase = phase;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public Guid MatchId { get; private set; }
    public Guid Player1CharacterId { get; private set; }
    public Guid Player2CharacterId { get; private set; }
    public int RoundNumber { get; private set; }
    public int Player1Health { get; private set; }
    public int Player2Health { get; private set; }
    public int Player1SuperBar { get; private set; }
    public int Player2SuperBar { get; private set; }
    public int Player1Wins { get; private set; }
    public int Player2Wins { get; private set; }
    public TimeSpan RoundTimeRemaining { get; private set; }
    public OpenMKMatchPhase Phase { get; private set; }
    public string? Player1CostumeName { get; private set; }
    public string? Player2CostumeName { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    public void UpdateState(
        int roundNumber,
        int player1Health,
        int player2Health,
        int player1SuperBar,
        int player2SuperBar,
        int player1Wins,
        int player2Wins,
        TimeSpan roundTimeRemaining,
        OpenMKMatchPhase phase)
    {
        RoundNumber = roundNumber;
        Player1Health = player1Health;
        Player2Health = player2Health;
        Player1SuperBar = player1SuperBar;
        Player2SuperBar = player2SuperBar;
        Player1Wins = player1Wins;
        Player2Wins = player2Wins;
        RoundTimeRemaining = roundTimeRemaining;
        Phase = phase;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public bool UpdateSuperBar(Guid characterId, int newValue)
    {
        if (characterId == Player1CharacterId)
        {
            Player1SuperBar = newValue;
            LastUpdatedAt = DateTime.UtcNow;
            return true;
        }

        if (characterId == Player2CharacterId)
        {
            Player2SuperBar = newValue;
            LastUpdatedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    public bool SetCostume(Guid characterId, string costumeName)
    {
        if (characterId == Player1CharacterId)
        {
            Player1CostumeName = costumeName;
            LastUpdatedAt = DateTime.UtcNow;
            return true;
        }

        if (characterId == Player2CharacterId)
        {
            Player2CostumeName = costumeName;
            LastUpdatedAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }
}
