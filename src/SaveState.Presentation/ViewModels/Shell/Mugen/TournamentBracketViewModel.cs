using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

/// <summary>
/// ViewModel for tournament bracket visualization.
/// </summary>
public partial class TournamentBracketViewModel : ObservableObject
{
    [ObservableProperty]
    private string _bracketTitle = "Tournament Bracket";

    [ObservableProperty]
    private int _roundCount;

    public ObservableCollection<object> Matches { get; } = new();

    public TournamentBracketViewModel()
    {
    }

    public TournamentBracketViewModel(string tournamentName, int participantCount)
    {
        BracketTitle = $"{tournamentName} - Bracket";
        RoundCount = (int)Math.Ceiling(Math.Log2(participantCount));
    }

    public void UpdateBracket()
    {
        // TODO: Implement bracket update logic
    }
}
