using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.Commands;
using SaveState.Application.Mugen.DTOs;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

public partial class MugenDeathBattleViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _player1;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _player2;

    [ObservableProperty]
    private string? _simulationReport;

    [ObservableProperty]
    private bool _isSimulating;

    public MugenDeathBattleViewModel(IMediator mediator)
    {
        _mediator = mediator;
        Title = "DEATH BATTLE SIMULATOR";
    }

    [RelayCommand]
    private async Task RunSimulationAsync()
    {
        if (Player1 == null || Player2 == null) return;

        try
        {
            IsSimulating = true;
            var result = await _mediator.Send(new RunDeathMatchSimulationCommand(Player1.Id, Player2.Id));

            if (result.IsSuccess && result.Value != null)
            {
                var sim = result.Value;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("DEATH BATTLE SIMULATION REPORT");
                sb.AppendLine("============================");
                sb.AppendLine($"{sim.Character1Name} vs {sim.Character2Name}");
                sb.AppendLine($"Total Matches Simulated: {sim.TotalSimulations}");
                sb.AppendLine($"Confidence: {sim.Confidence:P0}");
                sb.AppendLine();
                sb.AppendLine("ROUND BREAKDOWN:");
                foreach (var round in sim.RoundBreakdown)
                {
                    sb.AppendLine($"• Round {round.RoundNumber}: {round.PredictedWinner} wins (Factor: {round.KeyFactor})");
                }

                var winner = sim.Character1WinRate > sim.Character2WinRate ? sim.Character1Name : sim.Character2Name;
                var prob = Math.Max(sim.Character1WinRate, sim.Character2WinRate);
                sb.AppendLine();
                sb.AppendLine($"PREDICTED WINNER: {winner} ({prob:P0})");

                SimulationReport = sb.ToString();
            }
        }
        finally
        {
            IsSimulating = false;
        }
    }

    [RelayCommand]
    private async Task LaunchBattleAsync()
    {
        if (Player1 == null || Player2 == null) return;

        try
        {
            IsSimulating = true;
            SimulationReport = "Initiating battle...";

            // In a real app, this mediator command would call IMugenLauncher.LaunchVersusAsync
            await _mediator.Send(new LaunchIkemenVersusCommand(Player1.Name, Player2.Name));

            SimulationReport += "\nBattle started! Good luck, fighters.";
        }
        catch (Exception ex)
        {
            SimulationReport = $"Failed to launch: {ex.Message}";
        }
        finally
        {
            IsSimulating = false;
        }
    }
}

