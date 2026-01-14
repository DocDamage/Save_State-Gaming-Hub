using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using System.Collections.Generic;
using System.Linq;

namespace SaveState.Infrastructure.Tournaments;

/// <summary>
/// Tournament system for organizing gaming competitions.
/// PHASE 7: REQUIRED - Tournament System (Session 5)
/// </summary>
public class TournamentService
{
    private readonly ILogger<TournamentService> _logger;
    private readonly Dictionary<string, Tournament> _tournaments = new();

    public TournamentService(ILogger<TournamentService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new tournament.
    /// </summary>
    public async Task<Result<Tournament>> CreateTournamentAsync(
        string name,
        string gameId,
        int maxParticipants,
        TournamentFormat format,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating tournament: {TournamentName}", name);

            var tournament = new Tournament(
                id: Guid.NewGuid().ToString(),
                name: name,
                gameId: gameId,
                format: format,
                maxParticipants: maxParticipants,
                createdAt: DateTime.UtcNow,
                participants: new List<TournamentParticipant>(),
                status: TournamentStatus.Registration,
                bracket: null);

            _tournaments[tournament.Id] = tournament;

            _logger.LogInformation("Tournament created: {TournamentId}", tournament.Id);
            return Result.Success(tournament);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tournament: {TournamentName}", name);
            return Result.Failure<Tournament>(
                $"Tournament creation failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Registers a participant in a tournament.
    /// </summary>
    public async Task<Result> RegisterParticipantAsync(
        string tournamentId,
        string participantId,
        string participantName,
        CancellationToken ct = default)
    {
        try
        {
            if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            {
                return Result.Failure("Tournament not found", ErrorType.Validation);
            }

            if (tournament.Participants.Count >= tournament.MaxParticipants)
            {
                return Result.Failure("Tournament is full", ErrorType.Validation);
            }

            var participant = new TournamentParticipant(
                Id: participantId,
                Name: participantName,
                RegisteredAt: DateTime.UtcNow,
                Wins: 0,
                Losses: 0);

            tournament.Participants.Add(participant);

            _logger.LogInformation(
                "Participant registered for tournament {TournamentId}",
                tournamentId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register participant");
            return Result.Failure($"Registration failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Starts a tournament.
    /// </summary>
    public async Task<Result> StartTournamentAsync(
        string tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            {
                return Result.Failure("Tournament not found", ErrorType.Validation);
            }

            _logger.LogInformation("Starting tournament: {TournamentId}", tournamentId);

            tournament.Status = TournamentStatus.InProgress;
            tournament.Bracket = GenerateBracket(tournament);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start tournament: {TournamentId}", tournamentId);
            return Result.Failure($"Start failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Records match result.
    /// </summary>
    public async Task<Result> RecordMatchResultAsync(
        string tournamentId,
        string winnerId,
        string loserId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            {
                return Result.Failure("Tournament not found", ErrorType.Validation);
            }

            var winnerIndex = tournament.Participants.FindIndex(p => p.Id == winnerId);
            var loserIndex = tournament.Participants.FindIndex(p => p.Id == loserId);

            if (winnerIndex == -1 || loserIndex == -1)
            {
                return Result.Failure("Participant not found", ErrorType.Validation);
            }

            var winner = tournament.Participants[winnerIndex];
            var loser = tournament.Participants[loserIndex];

            // Update with new values
            tournament.Participants[winnerIndex] = winner with { Wins = winner.Wins + 1 };
            tournament.Participants[loserIndex] = loser with { Losses = loser.Losses + 1 };

            _logger.LogInformation(
                "Match recorded in tournament {TournamentId}",
                tournamentId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record match result");
            return Result.Failure($"Record failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Concludes a tournament.
    /// </summary>
    public async Task<Result<TournamentResults>> ConcludeTournamentAsync(
        string tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            {
                return Result.Failure<TournamentResults>(
                    "Tournament not found",
                    ErrorType.Validation);
            }

            _logger.LogInformation("Concluding tournament: {TournamentId}", tournamentId);

            tournament.Status = TournamentStatus.Completed;

            var ranking = tournament.Participants
                .OrderByDescending(p => p.Wins)
                .ThenBy(p => p.Losses)
                .ToList();

            var results = new TournamentResults(
                TournamentId: tournamentId,
                Winner: ranking.FirstOrDefault(),
                RunnerUp: ranking.Skip(1).FirstOrDefault(),
                ThirdPlace: ranking.Skip(2).FirstOrDefault(),
                FinalRanking: ranking,
                ConcludedAt: DateTime.UtcNow);

            return Result.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to conclude tournament: {TournamentId}", tournamentId);
            return Result.Failure<TournamentResults>(
                $"Conclusion failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets tournament leaderboard.
    /// </summary>
    public async Task<Result<List<LeaderboardEntry>>> GetLeaderboardAsync(
        string tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            {
                return Result.Failure<List<LeaderboardEntry>>(
                    "Tournament not found",
                    ErrorType.Validation);
            }

            var leaderboard = tournament.Participants
                .OrderByDescending(p => p.Wins)
                .ThenBy(p => p.Losses)
                .Select((p, index) => new LeaderboardEntry(
                    Rank: index + 1,
                    ParticipantName: p.Name,
                    Wins: p.Wins,
                    Losses: p.Losses))
                .ToList();

            return Result.Success(leaderboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaderboard");
            return Result.Failure<List<LeaderboardEntry>>(
                $"Leaderboard fetch failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    private TournamentBracket GenerateBracket(Tournament tournament)
    {
        return new TournamentBracket(
            TournamentId: tournament.Id,
            Format: tournament.Format,
            Rounds: new List<TournamentRound>(),
            GeneratedAt: DateTime.UtcNow);
    }
}

/// <summary>
/// Tournament.
/// </summary>
public class Tournament
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string GameId { get; set; }
    public TournamentFormat Format { get; set; }
    public int MaxParticipants { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TournamentParticipant> Participants { get; set; }
    public TournamentStatus Status { get; set; }
    public TournamentBracket? Bracket { get; set; }

    public Tournament(
        string id,
        string name,
        string gameId,
        TournamentFormat format,
        int maxParticipants,
        DateTime createdAt,
        List<TournamentParticipant> participants,
        TournamentStatus status,
        TournamentBracket? bracket)
    {
        Id = id;
        Name = name;
        GameId = gameId;
        Format = format;
        MaxParticipants = maxParticipants;
        CreatedAt = createdAt;
        Participants = participants;
        Status = status;
        Bracket = bracket;
    }
}

/// <summary>
/// Tournament participant.
/// </summary>
public record TournamentParticipant(
    string Id,
    string Name,
    DateTime RegisteredAt,
    int Wins,
    int Losses);

/// <summary>
/// Tournament format.
/// </summary>
public enum TournamentFormat
{
    SingleElimination,
    DoubleElimination,
    RoundRobin,
    Swiss
}

/// <summary>
/// Tournament status.
/// </summary>
public enum TournamentStatus
{
    Registration,
    InProgress,
    Completed
}

/// <summary>
/// Tournament bracket.
/// </summary>
public record TournamentBracket(
    string TournamentId,
    TournamentFormat Format,
    List<TournamentRound> Rounds,
    DateTime GeneratedAt);

/// <summary>
/// Tournament round.
/// </summary>
public record TournamentRound(
    int RoundNumber,
    List<TournamentMatch> Matches);

/// <summary>
/// Tournament match.
/// </summary>
public record TournamentMatch(
    string Id,
    string Participant1Id,
    string Participant2Id,
    string? WinnerId = null);

/// <summary>
/// Tournament results.
/// </summary>
public record TournamentResults(
    string TournamentId,
    TournamentParticipant? Winner,
    TournamentParticipant? RunnerUp,
    TournamentParticipant? ThirdPlace,
    List<TournamentParticipant> FinalRanking,
    DateTime ConcludedAt);

/// <summary>
/// Leaderboard entry.
/// </summary>
public record LeaderboardEntry(
    int Rank,
    string ParticipantName,
    int Wins,
    int Losses);
