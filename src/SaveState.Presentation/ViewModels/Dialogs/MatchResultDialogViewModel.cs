using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.TournamentManagement.Models;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the match result dialog.
/// </summary>
public partial class MatchResultDialogViewModel : ObservableObject
{
    private readonly TournamentMatch _match;

    [ObservableProperty]
    private string _player1Name = string.Empty;

    [ObservableProperty]
    private string _player2Name = string.Empty;

    [ObservableProperty]
    private int _player1Score;

    [ObservableProperty]
    private int _player2Score;

    [ObservableProperty]
    private string? _winnerId;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string? _replayPath;

    [ObservableProperty]
    private ObservableCollection<GameResultViewModel> _gameResults = new();

    [ObservableProperty]
    private int _bestOf = 3;

    [ObservableProperty]
    private string _validationError = string.Empty;

    /// <summary>
    /// Gets whether the result can be submitted.
    /// </summary>
    public bool CanSubmit => !string.IsNullOrEmpty(WinnerId) && HasValidScores();

    /// <summary>
    /// Gets the match ID.
    /// </summary>
    public string MatchId => _match.Id;

    public MatchResultDialogViewModel(TournamentMatch match)
    {
        _match = match;
        Player1Name = match.Participant1Name ?? "Player 1";
        Player2Name = match.Participant2Name ?? "Player 2";

        // Initialize game results based on best-of
        InitializeGameResults();
    }

    private void InitializeGameResults()
    {
        GameResults.Clear();
        int gamesNeeded = (BestOf / 2) + 1;
        for (int i = 1; i <= BestOf; i++)
        {
            GameResults.Add(new GameResultViewModel
            {
                GameNumber = i,
                Player1Score = null,
                Player2Score = null
            });
        }
    }

    partial void OnPlayer1ScoreChanged(int value)
    {
        ValidateAndDetermineWinner();
    }

    partial void OnPlayer2ScoreChanged(int value)
    {
        ValidateAndDetermineWinner();
    }

    partial void OnGameResultsChanged(ObservableCollection<GameResultViewModel> value)
    {
        ValidateAndDetermineWinner();
    }

    private bool HasValidScores()
    {
        // Check if overall scores make sense
        if (Player1Score < 0 || Player2Score < 0) return false;
        if (Player1Score == 0 && Player2Score == 0) return false;

        // For best-of, check if scores match the game results
        int p1Wins = GameResults.Count(g => g.Player1Score > g.Player2Score);
        int p2Wins = GameResults.Count(g => g.Player2Score > g.Player1Score);

        // Either overall score matches or game results are used
        return (Player1Score == p1Wins && Player2Score == p2Wins) ||
               (Player1Score > 0 || Player2Score > 0);
    }

    private void ValidateAndDetermineWinner()
    {
        // Determine winner based on scores
        if (Player1Score > Player2Score)
        {
            WinnerId = _match.Participant1Id;
        }
        else if (Player2Score > Player1Score)
        {
            WinnerId = _match.Participant2Id;
        }
        else
        {
            WinnerId = null;
        }

        // Update validation
        if (Player1Score == 0 && Player2Score == 0)
        {
            ValidationError = "Please enter scores for both players.";
        }
        else if (Player1Score == Player2Score)
        {
            ValidationError = "Scores cannot be tied (unless draws are allowed).";
        }
        else
        {
            ValidationError = string.Empty;
        }

        OnPropertyChanged(nameof(CanSubmit));
    }

    [RelayCommand]
    private void SetWinner(string player)
    {
        WinnerId = player == "1" ? _match.Participant1Id : _match.Participant2Id;

        // Auto-set scores if not set
        if (Player1Score == 0 && Player2Score == 0)
        {
            if (player == "1")
            {
                Player1Score = (BestOf / 2) + 1;
                Player2Score = 0;
            }
            else
            {
                Player1Score = 0;
                Player2Score = (BestOf / 2) + 1;
            }
        }

        OnPropertyChanged(nameof(CanSubmit));
    }

    [RelayCommand]
    private void SelectReplay()
    {
        // Open file picker for replay
        var dialog = new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Select Replay File",
            AllowMultiple = false,
            FileTypeFilter = new List<Avalonia.Platform.Storage.FilePickerFileType>
            {
                new("Replay Files") { Patterns = new[] { "*.rpl", "*.replay", "*.mkv", "*.mp4" } },
                new("All Files") { Patterns = new[] { "*" } }
            }
        };

        // This would need to be implemented with a proper file service
        // For now, we'll just simulate
    }

    [RelayCommand]
    private void Submit()
    {
        if (!CanSubmit) return;

        var gameResultList = GameResults
            .Where(g => g.Player1Score.HasValue && g.Player2Score.HasValue)
            .Select(g => new GameResult
            {
                GameNumber = g.GameNumber,
                Participant1Score = g.Player1Score,
                Participant2Score = g.Player2Score
            })
            .ToList();

        var result = new MatchResultDialogResult(
            MatchId: _match.Id,
            Player1Score: Player1Score,
            Player2Score: Player2Score,
            WinnerId: WinnerId!,
            GameResults: gameResultList,
            ReplayPath: ReplayPath,
            Notes: Notes.Trim());

        CloseDialog(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(null);
    }

    private void CloseDialog(MatchResultDialogResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}

/// <summary>
/// ViewModel for individual game results.
/// </summary>
public partial class GameResultViewModel : ObservableObject
{
    [ObservableProperty]
    private int _gameNumber;

    [ObservableProperty]
    private int? _player1Score;

    [ObservableProperty]
    private int? _player2Score;

    public string GameLabel => $"Game {GameNumber}";
}


