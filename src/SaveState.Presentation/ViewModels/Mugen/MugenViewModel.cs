using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.Commands;
using SaveState.Application.Mugen.DTOs;
using SaveState.Application.Mugen.Queries;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;

namespace SaveState.Presentation.ViewModels.Mugen;

public partial class MugenViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private MainViewModel? _mainViewModel;
    private readonly SaveState.Presentation.Resources.Resources _resources;
    private readonly MugenOptions _mugenOptions;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _player1;

    [ObservableProperty]
    private MugenCharacterSummaryDto? _player2;

    [ObservableProperty]
    private string? _aiAdvice;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _currentSection = "Selection"; // Selection, DeathBattle, Network, Fusion

    [ObservableProperty]
    private bool _isNetworkConnected;

    public ObservableCollection<LobbyDto> OnlineLobbies { get; } = new();

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public ObservableCollection<MugenCharacterSummaryDto> Characters { get; } = new();

    public bool HasCharacters => Characters.Count > 0;
    public bool HasNoCharacters => !HasCharacters;

    public MugenViewModel(
        IMediator mediator,
        SaveState.Presentation.Resources.Resources resources,
        IOptions<MugenOptions> mugenOptions)
    {
        _mediator = mediator;
        _resources = resources;
        _mugenOptions = mugenOptions.Value;
        LoadCharactersCommand = new AsyncRelayCommand(LoadCharactersAsync);
        ScanCharactersCommand = new AsyncRelayCommand(ScanCharactersAsync);
        LaunchCharacterCommand = new AsyncRelayCommand<MugenCharacterSummaryDto>(LaunchCharacterAsync);
        GetAdviceCommand = new AsyncRelayCommand<MugenCharacterSummaryDto>(GetAdviceAsync);
        ClearAdviceCommand = new RelayCommand(ClearAdvice);

        SelectPlayerCommand = new RelayCommand<MugenCharacterSummaryDto>(SelectPlayer);
        RunDeathBattleCommand = new AsyncRelayCommand(RunDeathBattleAsync);
        ToggleNetworkCommand = new AsyncRelayCommand(ToggleNetworkAsync);
        CreateFusionCommand = new AsyncRelayCommand(CreateFusionAsync);
        SetSectionCommand = new RelayCommand<string>(SetSection);
        GoBackCommand = new RelayCommand(GoBack);

        // Auto-load
        _ = LoadCharactersAsync();
    }

    public IAsyncRelayCommand LoadCharactersCommand { get; }
    public IAsyncRelayCommand ScanCharactersCommand { get; }
    public IAsyncRelayCommand<MugenCharacterSummaryDto> LaunchCharacterCommand { get; }
    public IAsyncRelayCommand<MugenCharacterSummaryDto> GetAdviceCommand { get; }
    public IRelayCommand ClearAdviceCommand { get; }
    public IRelayCommand<MugenCharacterSummaryDto> SelectPlayerCommand { get; }
    public IAsyncRelayCommand RunDeathBattleCommand { get; }
    public IAsyncRelayCommand ToggleNetworkCommand { get; }
    public IAsyncRelayCommand CreateFusionCommand { get; }
    public IRelayCommand<string> SetSectionCommand { get; }
    public IRelayCommand GoBackCommand { get; }

    private async Task LoadCharactersAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading MUGEN characters...";

            var query = new GetMugenCharactersQuery();
            var results = await _mediator.Send(query);

            Characters.Clear();
            foreach (var character in results)
            {
                Characters.Add(character);
            }

            OnPropertyChanged(nameof(HasCharacters));
            OnPropertyChanged(nameof(HasNoCharacters));

            StatusMessage = $"Found {Characters.Count} characters.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ScanCharactersAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Scanning for MUGEN characters...";

            // Use configured directory from options
            var path = _mugenOptions.CharacterDirectories.FirstOrDefault() ?? "chars";

            await _mediator.Send(new ScanMugenCharactersCommand(path));
            await LoadCharactersAsync();

            StatusMessage = "Scan complete.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LaunchCharacterAsync(MugenCharacterSummaryDto? character)
    {
        if (character == null) return;

        try
        {
            StatusMessage = $"Launching {character.Name} in IKEMEN...";
            // Launch character vs KFM (default)
            await _mediator.Send(new LaunchIkemenVersusCommand(character.Name, "kfm"));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Launch failed: {ex.Message}";
        }
    }

    private async Task GetAdviceAsync(MugenCharacterSummaryDto? character)
    {
        if (character == null) return;

        try
        {
            IsLoading = true;
            AiAdvice = "Analyzing character...";

            // Find KFM ID for advice comparison
            var kfm = Characters.FirstOrDefault(c => c.Name.Contains("Kung Fu Man", StringComparison.OrdinalIgnoreCase));
            var opponentId = kfm?.Id ?? character.Id; // Fallback to self-matchup if KFM not found

            var result = await _mediator.Send(new GetMugenMatchupAdviceCommand(character.Id, opponentId));

            if (result.IsSuccess && result.Value != null)
            {
                var advice = result.Value;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Predicted Win Rate: {advice.PredictedWinRate:P0}");
                sb.AppendLine();
                sb.AppendLine("Tips:");
                foreach (var tip in advice.Tips) sb.AppendLine($"• {tip}");
                sb.AppendLine();
                sb.AppendLine("Key Moves:");
                foreach (var move in advice.KeyMoves) sb.AppendLine($"• {move}");
                sb.AppendLine();
                sb.AppendLine("Avoid:");
                foreach (var move in advice.MovesToAvoid) sb.AppendLine($"• {move}");

                AiAdvice = sb.ToString();
            }
            else
            {
                AiAdvice = $"Advice error: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            AiAdvice = $"Advice unavailable: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SetParent(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    private void ClearAdvice()
    {
        AiAdvice = null;
    }

    private void SelectPlayer(MugenCharacterSummaryDto? character)
    {
        if (character == null) return;

        if (Player1 == null) Player1 = character;
        else if (Player2 == null && character != Player1) Player2 = character;
        else if (character == Player1) Player1 = null;
        else if (character == Player2) Player2 = null;
        else Player1 = character; // Rotate if both set
    }

    private async Task RunDeathBattleAsync()
    {
        if (Player1 == null || Player2 == null)
        {
            StatusMessage = "Select two characters for Death Battle!";
            return;
        }

        try
        {
            IsLoading = true;
            CurrentSection = "DeathBattle";
            StatusMessage = $"Simulating Death Battle: {Player1.Name} vs {Player2.Name}...";

            var result = await _mediator.Send(new RunDeathMatchSimulationCommand(Player1.Id, Player2.Id));

            if (result.IsSuccess && result.Value != null)
            {
                var sim = result.Value;
                var winner = sim.Character1WinRate > sim.Character2WinRate ? sim.Character1Name : sim.Character2Name;
                var prob = Math.Max(sim.Character1WinRate, sim.Character2WinRate);

                StatusMessage = $"Winner: {winner} ({prob:P0} probability)";

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
                    sb.AppendLine($"Round {round.RoundNumber}: {round.PredictedWinner} wins (Factor: {round.KeyFactor})");
                }

                AiAdvice = sb.ToString();
            }
            else
            {
                StatusMessage = $"Simulation failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Simulation failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ToggleNetworkAsync()
    {
        IsLoading = true;
        await Task.Delay(1000); // Simulate network handshake
        IsNetworkConnected = !IsNetworkConnected;

        if (IsNetworkConnected)
        {
            StatusMessage = "Connected to MUGEN Online Hub";
            OnlineLobbies.Clear();
            OnlineLobbies.Add(new LobbyDto("Street Fighter Vets", "SFII Turbo", 4, 8));
            OnlineLobbies.Add(new LobbyDto("Marvel vs Capcom 2 Unlimited", "MVC2", 2, 2));
            OnlineLobbies.Add(new LobbyDto("Test Fusion Lobby", "Custom", 1, 10));
        }
        else
        {
            StatusMessage = "Disconnected from MUGEN Network";
            OnlineLobbies.Clear();
        }
        IsLoading = false;
    }

    private async Task CreateFusionAsync()
    {
        if (Player1 == null || Player2 == null)
        {
            StatusMessage = "Select two characters to find Fusion potential!";
            return;
        }

        StatusMessage = $"Analyzing Fusion: {Player1.Name} + {Player2.Name}...";
        CurrentSection = "Fusion";
        await Task.Delay(1500);
        AiAdvice = $"CHARACTER FUSION DATA:\n\nName Placeholder: {Player1.Name[..3]}{Player2.Name[^3..]}\nType: Balanced\nFusion Power: 85%\n\nAssets ready for generation.";
    }

    private void SetSection(string? section)
    {
        if (string.IsNullOrEmpty(section)) return;
        CurrentSection = section;
    }

    [RelayCommand]
    private void StartTraining()
    {
        StatusMessage = "MUGEN Training Mode started (Simulated)";
    }

    private void GoBack()
    {
        _mainViewModel?.NavigateToGameLibrary();
    }
}

public record LobbyDto(string Name, string GameMode, int Players, int MaxPlayers);
