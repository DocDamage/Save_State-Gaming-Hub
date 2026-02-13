using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Simulation and betting partial class for MugenHubViewModel.
/// </summary>
public partial class MugenHubViewModel
{
    [RelayCommand]
    private async Task SimulateTournamentAsync()
    {
        try
        {
            IsSimulationLoading = true;
            SimulationStatus = "Simulating tournament...";
            SimulationWinnerName = null;
            SimulationWinnerConfidence = 0;
            SimulationMatches.Clear();

            if (Characters.Count == 0)
                await LoadCharactersAsync();

            var participants = Characters
                .Where(c => c.IsFavorite)
                .Take(Math.Max(2, SimulationParticipants))
                .Select(c => c.Id)
                .ToList();

            if (participants.Count < 2)
            {
                participants = Characters.Take(Math.Max(2, SimulationParticipants))
                    .Select(c => c.Id)
                    .ToList();
            }

            if (participants.Count < 2)
            {
                SimulationStatus = "Not enough characters to simulate.";
                return;
            }

            var betCharacter = SelectedBetCharacter;
            var betInvalid = false;
            if (betCharacter != null && !participants.Contains(betCharacter.Id))
            {
                BetStatus = "Bet character not in simulation.";
                betCharacter = null;
                betInvalid = true;
            }

            var result = await _deathMatchSimulator.SimulateTournamentAsync(
                participants,
                TournamentFormat.SingleElimination,
                Math.Max(10, SimulationsPerMatch));

            if (!result.IsSuccess || result.Value == null)
            {
                SimulationStatus = result.Error ?? "Simulation failed.";
                return;
            }

            var simulation = result.Value;
            var nameMap = Characters.ToDictionary(c => c.Id, c => c.DisplayName);

            SimulationWinnerName = nameMap.TryGetValue(simulation.PredictedWinnerId, out var winnerName)
                ? winnerName
                : simulation.PredictedWinnerName;
            SimulationWinnerConfidence = simulation.WinnerConfidence;

            var betAmount = Math.Clamp(BetAmount, 0, SpectatorCredits);
            if (betCharacter != null && betAmount > 0)
            {
                var betName = nameMap.TryGetValue(betCharacter.Id, out var bn) ? bn : betCharacter.DisplayName;
                var betWon = betCharacter.Id == simulation.PredictedWinnerId;

                if (betCharacter.Id == simulation.PredictedWinnerId)
                {
                    SpectatorCredits += betAmount;
                    BetStatus = $"Bet won. +{betAmount} credits.";
                }
                else
                {
                    SpectatorCredits = Math.Max(0, SpectatorCredits - betAmount);
                    BetStatus = $"Bet lost. -{betAmount} credits.";
                }

                BetHistory.Insert(0, new BetRecord(betCharacter.Id, betName, betAmount, betWon, SpectatorCredits, DateTime.UtcNow));
                if (BetHistory.Count > 20)
                    BetHistory.RemoveAt(BetHistory.Count - 1);
                UpdateBetLeaderboard();
            }
            else if (!betInvalid)
            {
                BetStatus = "No bet placed.";
            }

            foreach (var bracket in simulation.Brackets)
            {
                foreach (var match in bracket.Matches)
                {
                    var p1Name = nameMap.TryGetValue(match.Participant1Id, out var n1) ? n1 : match.Participant1Id.ToString();
                    var p2Name = nameMap.TryGetValue(match.Participant2Id, out var n2) ? n2 : match.Participant2Id.ToString();
                    var winner = nameMap.TryGetValue(match.PredictedWinnerId, out var wn) ? wn : match.PredictedWinnerId.ToString();

                    SimulationMatches.Add(new SimulatedMatchSummary(
                        bracket.RoundName,
                        p1Name,
                        p2Name,
                        winner,
                        match.WinConfidence,
                        match.SimulatedP1Wins,
                        match.SimulatedP2Wins));
                }
            }

            SimulationStatus = "Simulation complete.";
        }
        catch (Exception ex)
        {
            SimulationStatus = $"Simulation failed: {ex.Message}";
        }
        finally
        {
            IsSimulationLoading = false;
        }
    }

    private void UpdateBetLeaderboard()
    {
        var leaderboard = BetHistory
            .GroupBy(bet => bet.CharacterId)
            .Select(group => new BetLeaderboardEntry(
                group.Key,
                group.First().CharacterName,
                group.Count(),
                group.Count(bet => bet.Won),
                group.Count(bet => !bet.Won)))
            .OrderByDescending(entry => entry.Wins)
            .ThenByDescending(entry => entry.Bets)
            .ToList();

        BetLeaderboard.Clear();
        foreach (var entry in leaderboard)
            BetLeaderboard.Add(entry);
    }
}
