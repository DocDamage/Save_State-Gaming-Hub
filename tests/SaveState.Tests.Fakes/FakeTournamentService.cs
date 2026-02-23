using System.Collections.Concurrent;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Esports.Models;
using SaveState.Core.Esports.Services;

namespace SaveState.Tests.Fakes;

/// <summary>
/// Fake implementation of ITournamentService for integration testing.
/// Provides in-memory tournament management with full CRUD operations,
/// participant registration, bracket generation, and match management.
/// </summary>
public class FakeTournamentService : ITournamentService
{
    private readonly ConcurrentDictionary<Guid, Tournament> _tournaments = new();
    private readonly ITimeProvider _timeProvider;

    public FakeTournamentService(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    #region Tournament Management

    public Task<Result<Tournament>> CreateTournamentAsync(CreateTournamentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Task.FromResult(Result<Tournament>.Failure("Tournament name is required", ErrorType.Validation));

        if (request.MaxParticipants < 2)
            return Task.FromResult(Result<Tournament>.Failure("Max participants must be at least 2", ErrorType.Validation));

        var tournament = new Tournament
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Game = request.Game,
            Format = request.Format,
            Status = TournamentStatus.Draft,
            StartDate = request.StartDate,
            EndDate = null,
            RegistrationDeadline = request.RegistrationDeadline,
            MaxParticipants = request.MaxParticipants,
            MinParticipants = 2,
            Participants = new List<Participant>(),
            Rules = request.Rules ?? new TournamentRules(),
            PrizePool = request.PrizePool,
            Matches = new List<Match>(),
            CreatedBy = "test_user",
            CreatedAt = _timeProvider.UtcNow
        };

        _tournaments[tournament.Id] = tournament;
        return Task.FromResult(Result<Tournament>.Success(tournament));
    }

    public Task<Result<Tournament>> UpdateTournamentAsync(Guid tournamentId, UpdateTournamentRequest request, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<Tournament>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound));

        var updatedTournament = tournament with
        {
            Name = request.Name ?? tournament.Name,
            Description = request.Description ?? tournament.Description,
            StartDate = request.StartDate ?? tournament.StartDate,
            RegistrationDeadline = request.RegistrationDeadline ?? tournament.RegistrationDeadline,
            MaxParticipants = request.MaxParticipants ?? tournament.MaxParticipants,
            Rules = request.Rules ?? tournament.Rules,
            StreamUrl = request.StreamUrl ?? tournament.StreamUrl
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result<Tournament>.Success(updatedTournament));
    }

    public Task<Result> DeleteTournamentAsync(Guid tournamentId, CancellationToken ct = default)
    {
        if (_tournaments.TryRemove(tournamentId, out _))
            return Task.FromResult(Result.Success());

        return Task.FromResult(Result.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound));
    }

    public Task<Result<Tournament>> GetTournamentAsync(Guid tournamentId, CancellationToken ct = default)
    {
        if (_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<Tournament>.Success(tournament));

        return Task.FromResult(Result<Tournament>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound));
    }

    public Task<Result<IReadOnlyList<Tournament>>> GetTournamentsAsync(TournamentFilter filter, CancellationToken ct = default)
    {
        var query = _tournaments.Values.AsEnumerable();

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);

        if (filter.Format.HasValue)
            query = query.Where(t => t.Format == filter.Format.Value);

        if (filter.GameId.HasValue)
            query = query.Where(t => t.Game.GameId == filter.GameId.Value);

        if (filter.StartDateFrom.HasValue)
            query = query.Where(t => t.StartDate >= filter.StartDateFrom.Value);

        if (filter.StartDateTo.HasValue)
            query = query.Where(t => t.StartDate <= filter.StartDateTo.Value);

        if (!string.IsNullOrEmpty(filter.CreatedBy))
            query = query.Where(t => t.CreatedBy == filter.CreatedBy);

        if (!filter.IncludeCompleted)
            query = query.Where(t => t.Status != TournamentStatus.Completed);

        var result = query.OrderByDescending(t => t.CreatedAt).ToList();
        return Task.FromResult(Result<IReadOnlyList<Tournament>>.Success(result));
    }

    #endregion

    #region Participant Registration

    public Task<Result<Participant>> RegisterParticipantAsync(Guid tournamentId, RegisterParticipantRequest request, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<Participant>.Failure("Tournament not found", ErrorType.NotFound));

        if (tournament.Participants.Count >= tournament.MaxParticipants)
            return Task.FromResult(Result<Participant>.Failure("Tournament is full", ErrorType.Validation));

        if (tournament.Participants.Any(p => p.UserId == request.UserId))
            return Task.FromResult(Result<Participant>.Failure("User is already registered", ErrorType.Validation));

        var participant = new Participant
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            DisplayName = request.DisplayName,
            Seed = request.Seed ?? tournament.Participants.Count + 1,
            Status = ParticipantStatus.Registered,
            RegisteredAt = _timeProvider.UtcNow,
            CheckInCode = GenerateCheckInCode(),
            MatchHistory = new List<MatchResult>(),
            Wins = 0,
            Losses = 0,
            Ties = 0
        };

        var updatedParticipants = tournament.Participants.ToList();
        updatedParticipants.Add(participant);

        var updatedTournament = tournament with
        {
            Participants = updatedParticipants
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result<Participant>.Success(participant));
    }

    public Task<Result> UnregisterParticipantAsync(Guid tournamentId, Guid participantId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var participant = tournament.Participants.FirstOrDefault(p => p.Id == participantId);
        if (participant == null)
            return Task.FromResult(Result.Failure("Participant not found", ErrorType.NotFound));

        var updatedParticipants = tournament.Participants.Where(p => p.Id != participantId).ToList();

        // Re-seed remaining participants
        for (int i = 0; i < updatedParticipants.Count; i++)
        {
            updatedParticipants[i] = updatedParticipants[i] with { Seed = i + 1 };
        }

        var updatedTournament = tournament with
        {
            Participants = updatedParticipants
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> CheckInParticipantAsync(Guid tournamentId, Guid participantId, string checkInCode, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var participant = tournament.Participants.FirstOrDefault(p => p.Id == participantId);
        if (participant == null)
            return Task.FromResult(Result.Failure("Participant not found", ErrorType.NotFound));

        if (participant.CheckInCode != checkInCode)
            return Task.FromResult(Result.Failure("Invalid check-in code", ErrorType.Validation));

        var updatedParticipant = participant with
        {
            Status = ParticipantStatus.CheckedIn,
            CheckedInAt = _timeProvider.UtcNow
        };

        var updatedParticipants = tournament.Participants.Select(p =>
            p.Id == participantId ? updatedParticipant : p).ToList();

        var updatedTournament = tournament with
        {
            Participants = updatedParticipants
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Bracket Management

    public Task<Result<Bracket>> GenerateBracketAsync(Guid tournamentId, BracketOptions options, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<Bracket>.Failure("Tournament not found", ErrorType.NotFound));

        if (tournament.Participants.Count < 2)
            return Task.FromResult(Result<Bracket>.Failure("Not enough participants", ErrorType.Validation));

        var participants = options.RandomizeSeeds
            ? tournament.Participants.OrderBy(_ => Guid.NewGuid()).ToList()
            : tournament.Participants.OrderBy(p => p.Seed).ToList();

        var bracket = tournament.Format switch
        {
            TournamentFormat.SingleElimination => GenerateSingleEliminationBracket(tournament, participants),
            TournamentFormat.DoubleElimination => GenerateSingleEliminationBracket(tournament, participants), // Simplified
            TournamentFormat.RoundRobin => GenerateRoundRobinBracket(tournament, participants),
            TournamentFormat.Swiss => GenerateSwissBracket(tournament, participants),
            _ => GenerateSingleEliminationBracket(tournament, participants)
        };

        var updatedTournament = tournament with
        {
            Bracket = bracket,
            Matches = bracket.Matches.ToList()
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result<Bracket>.Success(bracket));
    }

    public Task<Result<Bracket>> GetBracketAsync(Guid tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<Bracket>.Failure("Tournament not found", ErrorType.NotFound));

        if (tournament.Bracket == null)
            return Task.FromResult(Result<Bracket>.Failure("Bracket not generated", ErrorType.NotFound));

        return Task.FromResult(Result<Bracket>.Success(tournament.Bracket));
    }

    public Task<Result> ResetBracketAsync(Guid tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var updatedTournament = tournament with
        {
            Bracket = null,
            Matches = new List<Match>()
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result.Success());
    }

    private Bracket GenerateSingleEliminationBracket(Tournament tournament, List<Participant> participants)
    {
        var participantCount = participants.Count;
        var rounds = (int)Math.Ceiling(Math.Log2(participantCount));
        var totalSlots = (int)Math.Pow(2, rounds);
        var byes = totalSlots - participantCount;

        var matches = new List<Match>();
        var roundsList = new List<BracketRound>();

        // Round 1 matches
        var round1Matches = new List<Match>();
        int matchNumber = 1;

        for (int i = 0; i < totalSlots / 2; i++)
        {
            Match match;
            if (i < byes)
            {
                // Top seed gets a bye
                var participant = participants[i];
                match = new Match
                {
                    Id = Guid.NewGuid(),
                    Round = 1,
                    MatchNumber = matchNumber++,
                    Status = MatchStatus.Completed,
                    Player1 = participant,
                    Winner = participant,
                    IsWinnersBracket = true
                };
            }
            else
            {
                var p1Index = i;
                var p2Index = totalSlots - 1 - i;
                var p1 = p1Index < participants.Count ? participants[p1Index] : null;
                var p2 = p2Index < participants.Count ? participants[p2Index] : null;

                match = new Match
                {
                    Id = Guid.NewGuid(),
                    Round = 1,
                    MatchNumber = matchNumber++,
                    Status = p1 != null && p2 != null ? MatchStatus.Scheduled : MatchStatus.Scheduled,
                    Player1 = p1,
                    Player2 = p2,
                    IsWinnersBracket = true
                };
            }

            round1Matches.Add(match);
            matches.Add(match);
        }

        roundsList.Add(new BracketRound
        {
            RoundNumber = 1,
            Name = "Round 1",
            Type = BracketType.Winners,
            Matches = round1Matches.ToList()
        });

        // Generate subsequent rounds
        var currentRoundMatches = round1Matches;
        for (int round = 2; round <= rounds; round++)
        {
            var nextRoundMatches = new List<Match>();
            for (int i = 0; i < currentRoundMatches.Count / 2; i++)
            {
                var match = new Match
                {
                    Id = Guid.NewGuid(),
                    Round = round,
                    MatchNumber = matchNumber++,
                    Status = MatchStatus.Scheduled,
                    IsWinnersBracket = true
                };

                // Link previous matches
                currentRoundMatches[i * 2].NextMatchWin = match.Id;
                currentRoundMatches[i * 2 + 1].NextMatchWin = match.Id;

                nextRoundMatches.Add(match);
                matches.Add(match);
            }

            roundsList.Add(new BracketRound
            {
                RoundNumber = round,
                Name = round == rounds ? "Finals" : $"Round {round}",
                Type = BracketType.Winners,
                Matches = nextRoundMatches.ToList()
            });

            currentRoundMatches = nextRoundMatches;
        }

        return new Bracket
        {
            Id = Guid.NewGuid(),
            Rounds = roundsList,
            Matches = matches,
            TotalRounds = rounds
        };
    }

    private Bracket GenerateRoundRobinBracket(Tournament tournament, List<Participant> participants)
    {
        var matches = new List<Match>();
        var rounds = new List<BracketRound>();

        int roundCount = participants.Count - 1;
        int matchNumber = 1;

        for (int round = 1; round <= roundCount; round++)
        {
            var roundMatches = new List<Match>();

            for (int i = 0; i < participants.Count / 2; i++)
            {
                var p1Index = (round + i) % participants.Count;
                var p2Index = (round + participants.Count - 1 - i) % participants.Count;

                var match = new Match
                {
                    Id = Guid.NewGuid(),
                    Round = round,
                    MatchNumber = matchNumber++,
                    Status = MatchStatus.Scheduled,
                    Player1 = participants[p1Index],
                    Player2 = participants[p2Index],
                    IsWinnersBracket = true
                };

                matches.Add(match);
                roundMatches.Add(match);
            }

            rounds.Add(new BracketRound
            {
                RoundNumber = round,
                Name = $"Round {round}",
                Type = BracketType.Winners,
                Matches = roundMatches
            });
        }

        return new Bracket
        {
            Id = Guid.NewGuid(),
            Rounds = rounds,
            Matches = matches,
            TotalRounds = roundCount
        };
    }

    private Bracket GenerateSwissBracket(Tournament tournament, List<Participant> participants)
    {
        var rounds = Math.Min(5, (int)Math.Ceiling(Math.Log2(participants.Count)));
        var matches = new List<Match>();
        var roundsList = new List<BracketRound>();

        // Swiss round 1: Random pairings
        var shuffled = participants.OrderBy(_ => Guid.NewGuid()).ToList();

        var round1Matches = new List<Match>();
        for (int i = 0; i < shuffled.Count / 2; i++)
        {
            var match = new Match
            {
                Id = Guid.NewGuid(),
                Round = 1,
                MatchNumber = i + 1,
                Status = MatchStatus.Scheduled,
                Player1 = shuffled[i * 2],
                Player2 = shuffled[i * 2 + 1],
                IsWinnersBracket = true
            };

            round1Matches.Add(match);
            matches.Add(match);
        }

        roundsList.Add(new BracketRound
        {
            RoundNumber = 1,
            Name = "Round 1",
            Type = BracketType.Winners,
            Matches = round1Matches
        });

        return new Bracket
        {
            Id = Guid.NewGuid(),
            Rounds = roundsList,
            Matches = matches,
            TotalRounds = rounds
        };
    }

    #endregion

    #region Match Management

    public Task<Result<Match>> ScheduleMatchAsync(Guid tournamentId, Guid matchId, ScheduleMatchRequest request, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<Match>.Failure("Tournament not found", ErrorType.NotFound));

        var match = tournament.Matches.FirstOrDefault(m => m.Id == matchId);
        if (match == null)
            return Task.FromResult(Result<Match>.Failure("Match not found", ErrorType.NotFound));

        var updatedMatch = match with
        {
            ScheduledTime = request.ScheduledTime,
            StreamUrl = request.StreamUrl
        };

        var updatedMatches = tournament.Matches.Select(m =>
            m.Id == matchId ? updatedMatch : m).ToList();

        var updatedTournament = tournament with
        {
            Matches = updatedMatches
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result<Match>.Success(updatedMatch));
    }

    public Task<Result<Match>> ReportMatchResultAsync(Guid tournamentId, Guid matchId, ReportMatchResultRequest request, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<Match>.Failure("Tournament not found", ErrorType.NotFound));

        var match = tournament.Matches.FirstOrDefault(m => m.Id == matchId);
        if (match == null)
            return Task.FromResult(Result<Match>.Failure("Match not found", ErrorType.NotFound));

        Participant? winner = null;
        if (request.Player1Score > request.Player2Score && match.Player1 != null)
            winner = match.Player1;
        else if (request.Player2Score > request.Player1Score && match.Player2 != null)
            winner = match.Player2;

        var result = new MatchResult
        {
            Winner = winner ?? new Participant { DisplayName = "Unknown" },
            Player1Score = request.Player1Score,
            Player2Score = request.Player2Score,
            Type = MatchResultType.Normal,
            Notes = request.Notes
        };

        var updatedMatch = match with
        {
            Result = result,
            Winner = winner,
            Status = MatchStatus.Completed,
            CompletedTime = _timeProvider.UtcNow
        };

        var updatedMatches = tournament.Matches.Select(m =>
            m.Id == matchId ? updatedMatch : m).ToList();

        var updatedTournament = tournament with
        {
            Matches = updatedMatches
        };

        _tournaments[tournamentId] = updatedTournament;

        // Advance winner to next match if applicable
        if (winner != null && match.NextMatchWin.HasValue)
        {
            AdvanceWinnerToNextMatch(tournamentId, match.NextMatchWin.Value, winner);
        }

        return Task.FromResult(Result<Match>.Success(updatedMatch));
    }

    private void AdvanceWinnerToNextMatch(Guid tournamentId, Guid nextMatchId, Participant winner)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return;

        var nextMatch = tournament.Matches.FirstOrDefault(m => m.Id == nextMatchId);
        if (nextMatch == null)
            return;

        Match updatedNextMatch;
        if (nextMatch.Player1 == null)
        {
            updatedNextMatch = nextMatch with { Player1 = winner };
        }
        else if (nextMatch.Player2 == null)
        {
            updatedNextMatch = nextMatch with { Player2 = winner };
        }
        else
        {
            return; // Both slots filled
        }

        var updatedMatches = tournament.Matches.Select(m =>
            m.Id == nextMatchId ? updatedNextMatch : m).ToList();

        var updatedTournament = tournament with
        {
            Matches = updatedMatches
        };

        _tournaments[tournamentId] = updatedTournament;
    }

    public Task<Result> StartMatchAsync(Guid tournamentId, Guid matchId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var match = tournament.Matches.FirstOrDefault(m => m.Id == matchId);
        if (match == null)
            return Task.FromResult(Result.Failure("Match not found", ErrorType.NotFound));

        var updatedMatch = match with
        {
            Status = MatchStatus.InProgress,
            StartedTime = _timeProvider.UtcNow
        };

        var updatedMatches = tournament.Matches.Select(m =>
            m.Id == matchId ? updatedMatch : m).ToList();

        var updatedTournament = tournament with
        {
            Matches = updatedMatches
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DisputeMatchAsync(Guid tournamentId, Guid matchId, string reason, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var match = tournament.Matches.FirstOrDefault(m => m.Id == matchId);
        if (match == null)
            return Task.FromResult(Result.Failure("Match not found", ErrorType.NotFound));

        var updatedMatch = match with
        {
            Status = MatchStatus.Disputed
        };

        var updatedMatches = tournament.Matches.Select(m =>
            m.Id == matchId ? updatedMatch : m).ToList();

        var updatedTournament = tournament with
        {
            Matches = updatedMatches
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Tournament Operations

    public Task<Result> StartTournamentAsync(Guid tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        if (tournament.Participants.Count < tournament.MinParticipants)
            return Task.FromResult(Result.Failure("Not enough participants", ErrorType.Validation));

        if (tournament.Bracket == null)
        {
            // Auto-generate bracket if not exists
            var bracketResult = GenerateBracketAsync(tournamentId, new BracketOptions(), ct).Result;
            if (bracketResult.IsFailure)
                return Task.FromResult(Result.Failure(bracketResult.Error ?? "Failed to generate bracket", ErrorType.Internal));
        }

        var updatedTournament = tournament with
        {
            Status = TournamentStatus.InProgress
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> PauseTournamentAsync(Guid tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var updatedTournament = tournament with
        {
            Status = TournamentStatus.Paused
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ResumeTournamentAsync(Guid tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var updatedTournament = tournament with
        {
            Status = TournamentStatus.InProgress
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> CompleteTournamentAsync(Guid tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var updatedTournament = tournament with
        {
            Status = TournamentStatus.Completed,
            EndDate = _timeProvider.UtcNow
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> CancelTournamentAsync(Guid tournamentId, string reason, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var updatedTournament = tournament with
        {
            Status = TournamentStatus.Cancelled,
            EndDate = _timeProvider.UtcNow
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Standings & Stats

    public Task<Result<IReadOnlyList<Participant>>> GetStandingsAsync(Guid tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<IReadOnlyList<Participant>>.Failure("Tournament not found", ErrorType.NotFound));

        var standings = tournament.Participants
            .OrderByDescending(p => p.Wins)
            .ThenBy(p => p.Losses)
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<Participant>>.Success(standings));
    }

    public Task<Result<TournamentStatistics>> GetStatisticsAsync(Guid tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<TournamentStatistics>.Failure("Tournament not found", ErrorType.NotFound));

        var stats = new TournamentStatistics
        {
            TotalMatches = tournament.Matches.Count,
            CompletedMatches = tournament.Matches.Count(m => m.Status == MatchStatus.Completed),
            RegisteredParticipants = tournament.Participants.Count,
            CheckedInParticipants = tournament.Participants.Count(p => p.Status == ParticipantStatus.CheckedIn)
        };

        return Task.FromResult(Result<TournamentStatistics>.Success(stats));
    }

    #endregion

    #region Helper Methods

    private static string GenerateCheckInCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    #endregion
}
