using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

public partial class MugenTournamentViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IMugenTournamentService _tournamentService;
    private readonly IMugenCollectionService _collectionService;
    private readonly IMatchPredictionEngine _predictionEngine;
    private readonly IDeathMatchSimulator _matchSimulator;

    public ObservableCollection<MugenCharacterSummaryDto> Participants { get; } = new();

    [ObservableProperty]
    private string _tournamentName = "New Tournament";

    [ObservableProperty]
    private bool _isTournamentActive;

    [ObservableProperty]
    private string _statusMessage = "Ready to create tournament";

    [ObservableProperty]
    private MugenTournament? _currentTournament;

    [ObservableProperty]
    private int _spectatorCredits = 1000;

    [ObservableProperty]
    private int _betAmount = 50;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _selectedBetCharacter;

    [ObservableProperty]
    private bool _isSimulationActive;

    [ObservableProperty]
    private TournamentBracketViewModel? _bracketViewModel;

    public ObservableCollection<SimulatedMatchSummary> SimulationMatches { get; } = new();
    public ObservableCollection<BetRecord> BetHistory { get; } = new();

    public MugenTournamentViewModel(
        IMediator mediator,
        IMugenTournamentService tournamentService,
        IMugenCollectionService collectionService,
        IMatchPredictionEngine predictionEngine,
        IDeathMatchSimulator matchSimulator)
    {
        _mediator = mediator;
        _tournamentService = tournamentService;
        _collectionService = collectionService;
        _predictionEngine = predictionEngine;
        _matchSimulator = matchSimulator;
        Title = "TOURNAMENT MODE";
    }

    [RelayCommand]
    private async Task CreateTournamentAsync()
    {
        if (Participants.Count < 2)
        {
            StatusMessage = "Need at least 2 participants.";
            return;
        }

        try
        {
            StatusMessage = "Creating tournament...";
            var participantIds = Participants.Select(p => p.Id).ToList();

            var request = new CreateTournamentRequest(
                TournamentName,
                TournamentFormat.SingleElimination,
                participantIds
            );

            var result = await _tournamentService.CreateTournamentAsync(request);

            if (result.IsSuccess)
            {
                CurrentTournament = result.Value;
                IsTournamentActive = true;
                StatusMessage = $"Tournament '{_tournamentName}' created!";
                await StartTournamentAsync();
            }
            else
            {
                StatusMessage = $"Failed to create tournament: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task StartTournamentAsync()
    {
        if (CurrentTournament == null) return;

        var result = await _tournamentService.StartTournamentAsync(CurrentTournament.Id);
        if (result.IsSuccess)
        {
            StatusMessage = "Tournament started! Generating bracket...";
            await RefreshTournamentAsync();
        }
        else
        {
            StatusMessage = $"Failed to start: {result.Error}";
        }
    }

    [RelayCommand]
    private async Task RefreshTournamentAsync()
    {
        if (CurrentTournament == null) return;

        var bracketResult = await _tournamentService.GetBracketAsync(CurrentTournament.Id);
        if (bracketResult.IsSuccess)
        {
            // Get character names for display
            var rosterResult = await _collectionService.GetRosterAsync();
            var characterNames = rosterResult.IsSuccess && rosterResult.Value != null
                ? rosterResult.Value.ToDictionary(c => c.Id, c => c.Name)
                : null;

            // Update the bracket view model
            BracketViewModel = new TournamentBracketViewModel(CurrentTournament, characterNames);
            StatusMessage = "Bracket updated.";
        }
    }

    [RelayCommand]
    private async Task SimulateTournamentAsync()
    {
        if (Participants.Count < 2) return;

        IsSimulationActive = true;
        SimulationMatches.Clear();
        StatusMessage = "Simulating tournament matches...";

        try
        {
            // Simple simulation logic: pair up participants and predict
            var currentRound = Participants.ToList();
            var roundNum = 1;

            while (currentRound.Count > 1)
            {
                var nextRound = new List<MugenCharacterSummaryDto>();
                for (int i = 0; i < currentRound.Count; i += 2)
                {
                    if (i + 1 >= currentRound.Count)
                    {
                        nextRound.Add(currentRound[i]);
                        continue;
                    }

                    var p1 = currentRound[i];
                    var p2 = currentRound[i + 1];

                    // Actual simulation/prediction
                    var simResultWrap = await _matchSimulator.SimulateMatchesAsync(p1.Id, p2.Id, 100);
                    if (simResultWrap.IsFailure || simResultWrap.Value == null)
                    {
                        nextRound.Add(p1); // Fallback
                        continue;
                    }

                    var simResult = simResultWrap.Value;
                    var winner = simResult.Character1Wins >= simResult.Character2Wins ? p1 : p2;
                    nextRound.Add(winner);

                    SimulationMatches.Add(new SimulatedMatchSummary(
                        $"Round {roundNum}",
                        p1.DisplayName,
                        p2.DisplayName,
                        winner.DisplayName,
                        simResult.Confidence,
                        simResult.Character1Wins,
                        simResult.Character2Wins));

                    await Task.Delay(200); // Visual feedback
                }
                currentRound = nextRound;
                roundNum++;
            }

            StatusMessage = $"Simulation complete! Winner: {currentRound[0].DisplayName}";

            // Resolve bets
            if (SelectedBetCharacter != null)
            {
                var won = currentRound[0].Id == SelectedBetCharacter.Id;
                var payout = won ? BetAmount * 2 : 0;
                SpectatorCredits += (payout - BetAmount);

                BetHistory.Add(new BetRecord(
                    SelectedBetCharacter.Id,
                    SelectedBetCharacter.DisplayName,
                    BetAmount,
                    won,
                    SpectatorCredits,
                    DateTime.Now));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Simulation error: {ex.Message}";
        }
        finally
        {
            IsSimulationActive = false;
        }
    }

    [RelayCommand]
    private void PlaceBet(MugenCharacterSummaryDto character)
    {
        if (SpectatorCredits < BetAmount)
        {
            StatusMessage = "Insufficient credits.";
            return;
        }
        SelectedBetCharacter = character;
        StatusMessage = $"Bet placed on {character.DisplayName} for {BetAmount} credits.";
    }

    [RelayCommand]
    private async Task AutoFillParticipantsAsync()
    {
        if (_collectionService == null)
        {
            StatusMessage = "Collection service unavailable.";
            return;
        }

        try
        {
             StatusMessage = "Auto-filling participants...";
             Participants.Clear();
             var result = await _collectionService.GetRosterAsync();
             if (result.IsSuccess && result.Value != null)
             {
                 // Pick random 8 or up to 8
                 var random = new Random();
                 var picked = result.Value.OrderBy(x => random.Next()).Take(8);
                 foreach (var character in picked)
                 {
                     Participants.Add(new MugenCharacterSummaryDto(
                         character.Id,
                         character.Name,
                         character.DisplayName,
                         character.Author,
                         character.Version,
                         character.IsValid,
                         character.LastScannedAt,
                         character.FileSize));
                 }
                 StatusMessage = $"Added {Participants.Count} random participants.";
             }
             else
             {
                 StatusMessage = "Failed to load roster.";
             }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddParticipant(MugenCharacterSummaryDto character)
    {
        if (!Participants.Any(p => p.Id == character.Id))
        {
            Participants.Add(character);
        }
    }

    [RelayCommand]
    private void RemoveParticipant(MugenCharacterSummaryDto character)
    {
        Participants.Remove(character);
    }
}
