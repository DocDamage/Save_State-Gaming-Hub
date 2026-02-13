using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

/// <summary>
/// ViewModel for tournament bracket visualization.
/// </summary>
public partial class TournamentBracketViewModel : ObservableObject
{
    private int _participantCount;

    [ObservableProperty]
    private string _tournamentName = "Tournament";

    [ObservableProperty]
    private string _status = "Waiting for bracket";

    [ObservableProperty]
    private string _bracketTitle = "Tournament Bracket";

    [ObservableProperty]
    private int _roundCount;

    public ObservableCollection<BracketRoundViewModel> Rounds { get; } = new();

    public TournamentBracketViewModel()
    {
    }

    public TournamentBracketViewModel(string tournamentName, int participantCount)
    {
        _participantCount = Math.Max(0, participantCount);
        TournamentName = tournamentName;
        BracketTitle = $"{tournamentName} - Bracket";
        RoundCount = _participantCount == 0
            ? 0
            : (int)Math.Ceiling(Math.Log2(_participantCount));
        UpdateBracket();
    }

    public void UpdateBracket()
    {
        Rounds.Clear();

        if (RoundCount == 0)
        {
            Status = "Awaiting participants";
            return;
        }

        var matchesInRound = (int)Math.Ceiling(_participantCount / 2.0);
        for (int round = 1; round <= RoundCount; round++)
        {
            var roundViewModel = new BracketRoundViewModel($"Round {round}");

            for (int matchIndex = 0; matchIndex < matchesInRound; matchIndex++)
            {
                roundViewModel.Matches.Add(new BracketMatchViewModel(
                    player1Name: "TBD",
                    player2Name: "TBD",
                    result: string.Empty,
                    isComplete: false));
            }

            Rounds.Add(roundViewModel);
            matchesInRound = Math.Max(1, matchesInRound / 2);
        }

        Status = $"Generated {Rounds.Count} rounds";
    }
}

public partial class BracketRoundViewModel : ObservableObject
{
    public BracketRoundViewModel(string roundName)
    {
        RoundName = roundName;
    }

    [ObservableProperty]
    private string _roundName;

    public ObservableCollection<BracketMatchViewModel> Matches { get; } = new();
}

public partial class BracketMatchViewModel : ObservableObject
{
    public BracketMatchViewModel(string player1Name, string player2Name, string result, bool isComplete)
    {
        Player1Name = player1Name;
        Player2Name = player2Name;
        Result = result;
        IsComplete = isComplete;
    }

    [ObservableProperty]
    private string _player1Name;

    [ObservableProperty]
    private string _player2Name;

    [ObservableProperty]
    private string _result;

    [ObservableProperty]
    private bool _isComplete;
}
