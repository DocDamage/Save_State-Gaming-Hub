using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.Netplay.Models;
using SaveState.Core.Netplay.Services;

namespace SaveState.Presentation.ViewModels.Netplay;

public partial class NetplayLobbyViewModel : ObservableObject
{
    private readonly IRetroNetplayService _netplayService;

    [ObservableProperty]
    private MatchmakingTicket? _currentTicket;

    [ObservableProperty]
    private bool _isMatchmaking;

    [ObservableProperty]
    private bool _isSpectatorMode;

    [ObservableProperty]
    private NetplayRegion _selectedRegion = NetplayRegion.NorthAmerica;

    [ObservableProperty]
    private SkillRating _selectedRating = SkillRating.Gold;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private NetplaySession? _selectedSession;

    [ObservableProperty]
    private LeaderboardEntry? _selectedLeaderboardEntry;

    public ObservableCollection<NetplaySession> ActiveSessions { get; }
    public ObservableCollection<LeaderboardEntry> LeaderboardEntries { get; }
    public ObservableCollection<NetplayRegion> Regions { get; }
    public ObservableCollection<SkillRating> Ratings { get; }

    public NetplayLobbyViewModel(IRetroNetplayService netplayService)
    {
        _netplayService = netplayService;

        // Initialize collections
        ActiveSessions = new ObservableCollection<NetplaySession>();
        LeaderboardEntries = new ObservableCollection<LeaderboardEntry>();
        Regions = new ObservableCollection<NetplayRegion>(Enum.GetValues<NetplayRegion>());
        Ratings = new ObservableCollection<SkillRating>(Enum.GetValues<SkillRating>());

        // Initialize
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await RefreshSessionsAsync();
        await RefreshLeaderboardAsync();
    }

    [RelayCommand]
    private async Task StartMatchmakingAsync()
    {
        try
        {
            StatusMessage = "Starting matchmaking...";
            IsMatchmaking = true;

            var request = new MatchmakingRequest
            {
                GameId = "retro-game-001",
                RomHash = new string('0', 64),
                Region = SelectedRegion,
                Rating = SelectedRating,
                PreferredRules = new List<string> { "Standard", "Best of 3" },
                MaxWaitTime = TimeSpan.FromMinutes(5)
            };

            var result = await _netplayService.StartMatchmakingAsync(request);

            if (result.IsSuccess)
            {
                CurrentTicket = result.Value;
                StatusMessage = $"Matchmaking started. Ticket: {CurrentTicket.TicketId}";
            }
            else
            {
                StatusMessage = $"Matchmaking failed: {result.Error}";
                IsMatchmaking = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsMatchmaking = false;
        }
    }

    [RelayCommand]
    private async Task CancelMatchmakingAsync()
    {
        if (CurrentTicket is null) return;

        try
        {
            StatusMessage = "Cancelling matchmaking...";
            var result = await _netplayService.CancelMatchmakingAsync(CurrentTicket.TicketId);

            if (result.IsSuccess)
            {
                StatusMessage = "Matchmaking cancelled";
                CurrentTicket = null;
                IsMatchmaking = false;
            }
            else
            {
                StatusMessage = $"Failed to cancel: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanConnectToSession))]
    private async Task ConnectToSessionAsync()
    {
        if (CurrentTicket is null) return;

        try
        {
            StatusMessage = "Connecting to peer...";
            var result = await _netplayService.ConnectToPeerAsync(CurrentTicket);

            if (result.IsSuccess)
            {
                var session = result.Value;
                StatusMessage = $"Connected to session {session.SessionId}";
                ActiveSessions.Add(session);
                CurrentTicket = null;
                IsMatchmaking = false;
            }
            else
            {
                StatusMessage = $"Connection failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private bool CanConnectToSession() => IsMatchmaking && CurrentTicket is not null;

    [RelayCommand]
    private async Task ToggleSpectatorModeAsync()
    {
        if (SelectedSession is null) return;

        try
        {
            if (IsSpectatorMode)
            {
                StatusMessage = "Starting spectator mode...";
                var result = await _netplayService.StartSpectatorModeAsync(SelectedSession.SessionId);
                StatusMessage = result.IsSuccess
                    ? "Spectator mode active"
                    : $"Failed to start spectator mode: {result.Error}";
            }
            else
            {
                StatusMessage = "Stopping spectator mode...";
                var result = await _netplayService.DisconnectAsync(SelectedSession.SessionId);
                StatusMessage = result.IsSuccess
                    ? "Spectator mode stopped"
                    : $"Failed to stop spectator mode: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        if (SelectedSession is null) return;

        try
        {
            StatusMessage = "Disconnecting...";
            var result = await _netplayService.DisconnectAsync(SelectedSession.SessionId);

            if (result.IsSuccess)
            {
                StatusMessage = "Disconnected successfully";
                ActiveSessions.Remove(SelectedSession);
                SelectedSession = null;
            }
            else
            {
                StatusMessage = $"Disconnect failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private bool CanDisconnect() => SelectedSession is not null;

    [RelayCommand]
    private async Task RefreshLeaderboardAsync()
    {
        try
        {
            var result = await _netplayService.GetLeaderboardAsync("retro-game-001");

            if (result.IsSuccess)
            {
                LeaderboardEntries.Clear();
                foreach (var entry in result.Value)
                {
                    LeaderboardEntries.Add(entry);
                }
                StatusMessage = "Leaderboard refreshed";
            }
            else
            {
                StatusMessage = $"Failed to refresh leaderboard: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshSessionsAsync()
    {
        try
        {
            var result = await _netplayService.GetActiveSessionsAsync();

            if (result.IsSuccess)
            {
                ActiveSessions.Clear();
                foreach (var session in result.Value)
                {
                    ActiveSessions.Add(session);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing sessions: {ex.Message}";
        }
    }
}
