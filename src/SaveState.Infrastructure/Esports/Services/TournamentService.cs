using System.Collections.Concurrent;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.TournamentManagement.Models;
using SaveState.Core.TournamentManagement.Services;

namespace SaveState.Infrastructure.Esports.Services;

/// <summary>
/// Implementation of the tournament management service using in-memory storage.
/// Provides full tournament lifecycle management including bracket generation,
/// match scheduling, and participant management.
/// </summary>
public class TournamentService : ITournamentManagementService
{
    private readonly ConcurrentDictionary<string, Tournament> _tournaments = new();
    private readonly ConcurrentDictionary<string, TournamentBracket> _brackets = new();
    private readonly ConcurrentDictionary<string, TournamentStandings> _standings = new();
    private readonly ConcurrentDictionary<string, TournamentSchedule> _schedules = new();
    private readonly ITimeProvider _timeProvider;

    public TournamentService(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    #region Tournament CRUD

    /// <inheritdoc />
    public Task<Result<Tournament>> CreateTournamentAsync(CreateTournamentRequest request, string organizerId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Task.FromResult(Result<Tournament>.Failure("Tournament name is required", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(request.GameId))
            return Task.FromResult(Result<Tournament>.Failure("Game is required", ErrorType.Validation));

        if (request.MaxParticipants < 2 || request.MaxParticipants > 512)
            return Task.FromResult(Result<Tournament>.Failure("Participants must be between 2 and 512", ErrorType.Validation));

        var tournament = new Tournament
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            GameId = request.GameId,
            GameName = request.GameId, // Would be populated from game service in real implementation
            Format = request.Format,
            Status = TournamentStatus.RegistrationOpen,
            RegistrationStart = request.RegistrationStart,
            RegistrationEnd = request.RegistrationEnd,
            TournamentStart = request.TournamentStart,
            MaxParticipants = request.MaxParticipants,
            CurrentParticipants = 0,
            OrganizerId = organizerId,
            OrganizerName = organizerId, // Would be populated from user service
            Rules = request.Rules,
            PrizePool = request.InitialPrizePool,
            Participants = Array.Empty<TournamentParticipant>(),
            Matches = Array.Empty<TournamentMatch>(),
            CreatedAt = _timeProvider.UtcNow
        };

        _tournaments[tournament.Id] = tournament;
        return Task.FromResult(Result<Tournament>.Success(tournament));
    }

    /// <inheritdoc />
    public Task<Result<Tournament>> GetTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        if (_tournaments.TryGetValue(tournamentId, out var tournament))
        {
            return Task.FromResult(Result<Tournament>.Success(tournament));
        }
        return Task.FromResult(Result<Tournament>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound));
    }

    /// <inheritdoc />
    public Task<Result<Tournament>> UpdateTournamentAsync(Tournament tournament, CancellationToken ct = default)
    {
        if (tournament == null)
            return Task.FromResult(Result<Tournament>.Failure("Tournament cannot be null", ErrorType.Validation));

        if (!_tournaments.ContainsKey(tournament.Id))
            return Task.FromResult(Result<Tournament>.Failure($"Tournament {tournament.Id} not found", ErrorType.NotFound));

        _tournaments[tournament.Id] = tournament;
        return Task.FromResult(Result<Tournament>.Success(tournament));
    }

    /// <inheritdoc />
    public Task<Result> DeleteTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        if (_tournaments.TryRemove(tournamentId, out _))
        {
            _brackets.TryRemove(tournamentId, out _);
            _standings.TryRemove(tournamentId, out _);
            _schedules.TryRemove(tournamentId, out _);
            return Task.FromResult(Result.Success());
        }
        return Task.FromResult(Result.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<Tournament>>> ListTournamentsAsync(TournamentStatus? status = null, string? gameId = null, string? organizerId = null, CancellationToken ct = default)
    {
        var query = _tournaments.Values.AsEnumerable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (!string.IsNullOrEmpty(gameId))
            query = query.Where(t => t.GameId == gameId);

        if (!string.IsNullOrEmpty(organizerId))
            query = query.Where(t => t.OrganizerId == organizerId);

        var result = query.OrderByDescending(t => t.CreatedAt).ToList();
        return Task.FromResult(Result<IReadOnlyList<Tournament>>.Success(result));
    }

    #endregion

    #region Participant Management

    /// <inheritdoc />
    public Task<Result<TournamentParticipant>> RegisterParticipantAsync(string tournamentId, string userId, string displayName, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<TournamentParticipant>.Failure("Tournament not found", ErrorType.NotFound));

        if (tournament.Status != TournamentStatus.RegistrationOpen)
            return Task.FromResult(Result<TournamentParticipant>.Failure("Registration is closed", ErrorType.Validation));

        if (tournament.CurrentParticipants >= tournament.MaxParticipants)
            return Task.FromResult(Result<TournamentParticipant>.Failure("Tournament is full", ErrorType.Validation));

        if (tournament.Participants.Any(p => p.UserId == userId))
            return Task.FromResult(Result<TournamentParticipant>.Failure("Already registered", ErrorType.Validation));

        var participant = new TournamentParticipant
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            DisplayName = displayName,
            Seed = tournament.CurrentParticipants + 1,
            Status = ParticipantStatus.Registered,
            RegisteredAt = _timeProvider.UtcNow
        };

        var updatedParticipants = tournament.Participants.ToList();
        updatedParticipants.Add(participant);

        var updatedTournament = tournament with
        {
            Participants = updatedParticipants,
            CurrentParticipants = updatedParticipants.Count
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result<TournamentParticipant>.Success(participant));
    }

    /// <inheritdoc />
    public Task<Result> UnregisterParticipantAsync(string tournamentId, string participantId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var participant = tournament.Participants.FirstOrDefault(p => p.Id == participantId);
        if (participant == null)
            return Task.FromResult(Result.Failure("Participant not found", ErrorType.NotFound));

        var updatedParticipants = tournament.Participants.Where(p => p.Id != participantId).ToList();

        // Re-seed participants
        for (int i = 0; i < updatedParticipants.Count; i++)
        {
            updatedParticipants[i] = updatedParticipants[i] with { Seed = i + 1 };
        }

        var updatedTournament = tournament with
        {
            Participants = updatedParticipants,
            CurrentParticipants = updatedParticipants.Count
        };

        _tournaments[tournamentId] = updatedTournament;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> CheckInParticipantAsync(string tournamentId, string participantId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var participant = tournament.Participants.FirstOrDefault(p => p.Id == participantId);
        if (participant == null)
            return Task.FromResult(Result.Failure("Participant not found", ErrorType.NotFound));

        var updatedParticipant = participant with { Status = ParticipantStatus.CheckedIn };
        var updatedParticipants = tournament.Participants.Select(p =>
            p.Id == participantId ? updatedParticipant : p).ToList();

        var updatedTournament = tournament with { Participants = updatedParticipants };
        _tournaments[tournamentId] = updatedTournament;

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> DisqualifyParticipantAsync(string tournamentId, string participantId, string reason, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var participant = tournament.Participants.FirstOrDefault(p => p.Id == participantId);
        if (participant == null)
            return Task.FromResult(Result.Failure("Participant not found", ErrorType.NotFound));

        var updatedParticipant = participant with { Status = ParticipantStatus.Disqualified };
        var updatedParticipants = tournament.Participants.Select(p =>
            p.Id == participantId ? updatedParticipant : p).ToList();

        var updatedTournament = tournament with { Participants = updatedParticipants };
        _tournaments[tournamentId] = updatedTournament;

        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Bracket Management

    /// <inheritdoc />
    public Task<Result<TournamentBracket>> GenerateBracketAsync(string tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<TournamentBracket>.Failure("Tournament not found", ErrorType.NotFound));

        if (tournament.Participants.Count < 2)
            return Task.FromResult(Result<TournamentBracket>.Failure("Not enough participants", ErrorType.Validation));

        var bracket = tournament.Format switch
        {
            TournamentFormat.SingleElimination => GenerateSingleEliminationBracket(tournament),
            TournamentFormat.DoubleElimination => GenerateDoubleEliminationBracket(tournament),
            TournamentFormat.RoundRobin => GenerateRoundRobinBracket(tournament),
            TournamentFormat.Swiss => GenerateSwissBracket(tournament),
            _ => GenerateSingleEliminationBracket(tournament)
        };

        _brackets[tournamentId] = bracket;

        // Update tournament with matches
        var updatedTournament = tournament with { Matches = bracket.Matches };
        _tournaments[tournamentId] = updatedTournament;

        return Task.FromResult(Result<TournamentBracket>.Success(bracket));
    }

    /// <inheritdoc />
    public Task<Result<TournamentBracket>> GetBracketAsync(string tournamentId, CancellationToken ct = default)
    {
        if (_brackets.TryGetValue(tournamentId, out var bracket))
        {
            return Task.FromResult(Result<TournamentBracket>.Success(bracket));
        }
        return Task.FromResult(Result<TournamentBracket>.Failure("Bracket not found", ErrorType.NotFound));
    }

    private TournamentBracket GenerateSingleEliminationBracket(Tournament tournament)
    {
        var participants = tournament.Participants.ToList();
        var participantCount = participants.Count;

        // Calculate number of rounds
        var rounds = (int)Math.Ceiling(Math.Log2(participantCount));
        var totalSlots = (int)Math.Pow(2, rounds);

        var matches = new List<TournamentMatch>();
        var roundsList = new List<BracketRound>();

        int matchNumber = 1;

        // Round 1: Byes for top seeds if needed
        var byes = totalSlots - participantCount;
        var activeParticipants = participants.ToList();

        // Generate round 1 matches
        var round1Matches = new List<TournamentMatch>();
        for (int i = 0; i < (totalSlots / 2); i++)
        {
            TournamentMatch? match;
            if (i < byes)
            {
                // Top seed gets a bye
                match = new TournamentMatch
                {
                    Id = Guid.NewGuid().ToString(),
                    TournamentId = tournament.Id,
                    Round = 1,
                    MatchNumber = matchNumber++,
                    BracketSection = "Winners",
                    Status = MatchStatus.Bye,
                    Participant1Id = participants[i].Id,
                    Participant1Name = participants[i].DisplayName,
                    Participant2Id = null,
                    Participant2Name = null,
                    WinnerId = participants[i].Id
                };
            }
            else
            {
                var p1Index = i;
                var p2Index = totalSlots - 1 - i;

                var p1 = p1Index < participants.Count ? participants[p1Index] : null;
                var p2 = p2Index < participants.Count ? participants[p2Index] : null;

                match = new TournamentMatch
                {
                    Id = Guid.NewGuid().ToString(),
                    TournamentId = tournament.Id,
                    Round = 1,
                    MatchNumber = matchNumber++,
                    BracketSection = "Winners",
                    Status = p1 != null && p2 != null ? MatchStatus.Scheduled : MatchStatus.Bye,
                    Participant1Id = p1?.Id,
                    Participant1Name = p1?.DisplayName,
                    Participant2Id = p2?.Id,
                    Participant2Name = p2?.DisplayName
                };
            }
            round1Matches.Add(match);
            matches.Add(match);
        }

        roundsList.Add(new BracketRound
        {
            RoundNumber = 1,
            Name = "Round 1",
            IsWinnersBracket = true,
            MatchesCount = round1Matches.Count,
            IsFinal = false
        });

        // Generate subsequent rounds
        var currentRoundMatches = round1Matches;
        for (int round = 2; round <= rounds; round++)
        {
            var nextRoundMatches = new List<TournamentMatch>();
            for (int i = 0; i < currentRoundMatches.Count / 2; i++)
            {
                var match = new TournamentMatch
                {
                    Id = Guid.NewGuid().ToString(),
                    TournamentId = tournament.Id,
                    Round = round,
                    MatchNumber = matchNumber++,
                    BracketSection = "Winners",
                    Status = MatchStatus.Scheduled
                };

                // Link previous matches
                currentRoundMatches[i * 2] = currentRoundMatches[i * 2] with { NextMatchId = match.Id, NextMatchSlot = "1" };
                currentRoundMatches[i * 2 + 1] = currentRoundMatches[i * 2 + 1] with { NextMatchId = match.Id, NextMatchSlot = "2" };

                nextRoundMatches.Add(match);
                matches.Add(match);
            }

            roundsList.Add(new BracketRound
            {
                RoundNumber = round,
                Name = round == rounds ? "Finals" : $"Round {round}",
                IsWinnersBracket = true,
                MatchesCount = nextRoundMatches.Count,
                IsFinal = round == rounds
            });

            currentRoundMatches = nextRoundMatches;
        }

        return new TournamentBracket
        {
            TournamentId = tournament.Id,
            Format = TournamentFormat.SingleElimination,
            Rounds = roundsList,
            Matches = matches,
            CurrentPosition = new BracketPosition
            {
                CurrentRound = 1,
                TotalRounds = rounds,
                MatchesCompleted = 0,
                MatchesRemaining = matches.Count
            }
        };
    }

    private TournamentBracket GenerateDoubleEliminationBracket(Tournament tournament)
    {
        // Simplified double elimination - just create winners bracket for now
        var winnersBracket = GenerateSingleEliminationBracket(tournament);
        return winnersBracket;
    }

    private TournamentBracket GenerateRoundRobinBracket(Tournament tournament)
    {
        var participants = tournament.Participants.ToList();
        var matches = new List<TournamentMatch>();
        var rounds = new List<BracketRound>();

        int roundCount = participants.Count - 1;
        int matchNumber = 1;

        for (int round = 1; round <= roundCount; round++)
        {
            var roundMatches = new List<TournamentMatch>();

            for (int i = 0; i < participants.Count / 2; i++)
            {
                var p1Index = (round + i) % participants.Count;
                var p2Index = (round + participants.Count - 1 - i) % participants.Count;

                var match = new TournamentMatch
                {
                    Id = Guid.NewGuid().ToString(),
                    TournamentId = tournament.Id,
                    Round = round,
                    MatchNumber = matchNumber++,
                    BracketSection = "Round Robin",
                    Status = MatchStatus.Scheduled,
                    Participant1Id = participants[p1Index].Id,
                    Participant1Name = participants[p1Index].DisplayName,
                    Participant2Id = participants[p2Index].Id,
                    Participant2Name = participants[p2Index].DisplayName
                };

                matches.Add(match);
                roundMatches.Add(match);
            }

            rounds.Add(new BracketRound
            {
                RoundNumber = round,
                Name = $"Round {round}",
                IsWinnersBracket = true,
                MatchesCount = roundMatches.Count,
                IsFinal = round == roundCount
            });
        }

        return new TournamentBracket
        {
            TournamentId = tournament.Id,
            Format = TournamentFormat.RoundRobin,
            Rounds = rounds,
            Matches = matches,
            CurrentPosition = new BracketPosition
            {
                CurrentRound = 1,
                TotalRounds = roundCount,
                MatchesCompleted = 0,
                MatchesRemaining = matches.Count
            }
        };
    }

    private TournamentBracket GenerateSwissBracket(Tournament tournament)
    {
        var participants = tournament.Participants.ToList();
        var rounds = Math.Min(5, (int)Math.Ceiling(Math.Log2(participants.Count)));

        var matches = new List<TournamentMatch>();
        var roundsList = new List<BracketRound>();

        // Swiss round 1: Random pairings
        var round1Matches = new List<TournamentMatch>();
        var random = new Random();
        var shuffled = participants.OrderBy(_ => random.Next()).ToList();

        for (int i = 0; i < shuffled.Count / 2; i++)
        {
            var match = new TournamentMatch
            {
                Id = Guid.NewGuid().ToString(),
                TournamentId = tournament.Id,
                Round = 1,
                MatchNumber = i + 1,
                BracketSection = "Swiss",
                Status = MatchStatus.Scheduled,
                Participant1Id = shuffled[i * 2].Id,
                Participant1Name = shuffled[i * 2].DisplayName,
                Participant2Id = shuffled[i * 2 + 1].Id,
                Participant2Name = shuffled[i * 2 + 1].DisplayName
            };
            round1Matches.Add(match);
            matches.Add(match);
        }

        roundsList.Add(new BracketRound
        {
            RoundNumber = 1,
            Name = "Round 1",
            IsWinnersBracket = true,
            MatchesCount = round1Matches.Count,
            IsFinal = false
        });

        return new TournamentBracket
        {
            TournamentId = tournament.Id,
            Format = TournamentFormat.Swiss,
            Rounds = roundsList,
            Matches = matches,
            CurrentPosition = new BracketPosition
            {
                CurrentRound = 1,
                TotalRounds = rounds,
                MatchesCompleted = 0,
                MatchesRemaining = matches.Count + (rounds - 1) * (participants.Count / 2)
            }
        };
    }

    #endregion

    #region Tournament Lifecycle

    /// <inheritdoc />
    public Task<Result> StartTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        if (tournament.Status != TournamentStatus.RegistrationOpen && tournament.Status != TournamentStatus.RegistrationClosed)
            return Task.FromResult(Result.Failure("Tournament cannot be started", ErrorType.Validation));

        if (tournament.Participants.Count < 2)
            return Task.FromResult(Result.Failure("Not enough participants", ErrorType.Validation));

        // Generate bracket if not exists
        if (!_brackets.ContainsKey(tournamentId))
        {
            var bracketResult = GenerateBracketAsync(tournamentId, ct).Result;
            if (bracketResult.IsFailure)
                return Task.FromResult(Result.Failure(bracketResult.Error ?? "Failed to generate bracket", ErrorType.Internal));
        }

        var updatedTournament = tournament with { Status = TournamentStatus.InProgress };
        _tournaments[tournamentId] = updatedTournament;

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> CompleteTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        if (tournament.Status != TournamentStatus.InProgress)
            return Task.FromResult(Result.Failure("Tournament is not in progress", ErrorType.Validation));

        var updatedTournament = tournament with { Status = TournamentStatus.Completed, TournamentEnd = _timeProvider.UtcNow };
        _tournaments[tournamentId] = updatedTournament;

        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Match Management

    /// <inheritdoc />
    public Task<Result<TournamentMatch>> ReportMatchResultAsync(string tournamentId, string matchId, int participant1Score, int participant2Score, string reporterId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<TournamentMatch>.Failure("Tournament not found", ErrorType.NotFound));

        var match = tournament.Matches.FirstOrDefault(m => m.Id == matchId);
        if (match == null)
            return Task.FromResult(Result<TournamentMatch>.Failure("Match not found", ErrorType.NotFound));

        string? winnerId = null;
        string? loserId = null;

        if (participant1Score > participant2Score)
        {
            winnerId = match.Participant1Id;
            loserId = match.Participant2Id;
        }
        else if (participant2Score > participant1Score)
        {
            winnerId = match.Participant2Id;
            loserId = match.Participant1Id;
        }

        var updatedMatch = match with
        {
            Participant1Score = participant1Score,
            Participant2Score = participant2Score,
            WinnerId = winnerId,
            LoserId = loserId,
            Status = MatchStatus.Completed,
            CompletedAt = _timeProvider.UtcNow
        };

        var updatedMatches = tournament.Matches.Select(m => m.Id == matchId ? updatedMatch : m).ToList();
        var updatedTournament = tournament with { Matches = updatedMatches };
        _tournaments[tournamentId] = updatedTournament;

        // Update bracket if exists
        if (_brackets.TryGetValue(tournamentId, out var bracket))
        {
            var updatedBracketMatches = bracket.Matches.Select(m => m.Id == matchId ? updatedMatch : m).ToList();
            _brackets[tournamentId] = bracket with { Matches = updatedBracketMatches };
        }

        return Task.FromResult(Result<TournamentMatch>.Success(updatedMatch));
    }

    /// <inheritdoc />
    public Task<Result<TournamentMatch>> ConfirmMatchResultAsync(string tournamentId, string matchId, string confirmerId, CancellationToken ct = default)
    {
        // In a real implementation, this would verify the confirmer is not the reporter
        // and mark the result as confirmed
        return GetMatchAsync(tournamentId, matchId, ct);
    }

    /// <inheritdoc />
    public Task<Result> AdvanceWinnerAsync(string tournamentId, string matchId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        var match = tournament.Matches.FirstOrDefault(m => m.Id == matchId);
        if (match == null)
            return Task.FromResult(Result.Failure("Match not found", ErrorType.NotFound));

        if (match.WinnerId == null || match.NextMatchId == null)
            return Task.FromResult(Result.Failure("No winner to advance or no next match", ErrorType.Validation));

        var nextMatch = tournament.Matches.FirstOrDefault(m => m.Id == match.NextMatchId);
        if (nextMatch == null)
            return Task.FromResult(Result.Failure("Next match not found", ErrorType.NotFound));

        // Update next match with winner
        TournamentMatch updatedNextMatch;
        if (match.NextMatchSlot == "1")
        {
            updatedNextMatch = nextMatch with
            {
                Participant1Id = match.WinnerId,
                Participant1Name = match.WinnerId == match.Participant1Id ? match.Participant1Name : match.Participant2Name,
                Status = nextMatch.Participant2Id != null ? MatchStatus.Ready : MatchStatus.Scheduled
            };
        }
        else
        {
            updatedNextMatch = nextMatch with
            {
                Participant2Id = match.WinnerId,
                Participant2Name = match.WinnerId == match.Participant1Id ? match.Participant1Name : match.Participant2Name,
                Status = nextMatch.Participant1Id != null ? MatchStatus.Ready : MatchStatus.Scheduled
            };
        }

        var updatedMatches = tournament.Matches.Select(m => m.Id == nextMatch.Id ? updatedNextMatch : m).ToList();
        var updatedTournament = tournament with { Matches = updatedMatches };
        _tournaments[tournamentId] = updatedTournament;

        // Update bracket
        if (_brackets.TryGetValue(tournamentId, out var bracket))
        {
            var updatedBracketMatches = bracket.Matches.Select(m => m.Id == nextMatch.Id ? updatedNextMatch : m).ToList();
            _brackets[tournamentId] = bracket with { Matches = updatedBracketMatches };
        }

        return Task.FromResult(Result.Success());
    }

    private Task<Result<TournamentMatch>> GetMatchAsync(string tournamentId, string matchId, CancellationToken ct)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<TournamentMatch>.Failure("Tournament not found", ErrorType.NotFound));

        var match = tournament.Matches.FirstOrDefault(m => m.Id == matchId);
        if (match == null)
            return Task.FromResult(Result<TournamentMatch>.Failure("Match not found", ErrorType.NotFound));

        return Task.FromResult(Result<TournamentMatch>.Success(match));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TournamentMatch>>> GetParticipantMatchesAsync(string tournamentId, string participantId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<IReadOnlyList<TournamentMatch>>.Failure("Tournament not found", ErrorType.NotFound));

        var matches = tournament.Matches.Where(m =>
            m.Participant1Id == participantId || m.Participant2Id == participantId).ToList();

        return Task.FromResult(Result<IReadOnlyList<TournamentMatch>>.Success(matches));
    }

    #endregion

    #region Schedule Management

    /// <inheritdoc />
    public Task<Result<TournamentSchedule>> GenerateScheduleAsync(string tournamentId, DateTime startTime, TimeSpan timeBetweenMatches, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<TournamentSchedule>.Failure("Tournament not found", ErrorType.NotFound));

        var scheduledMatches = new List<ScheduledMatch>();
        var currentTime = startTime;

        foreach (var match in tournament.Matches.OrderBy(m => m.Round).ThenBy(m => m.MatchNumber))
        {
            scheduledMatches.Add(new ScheduledMatch
            {
                MatchId = match.Id,
                ScheduledTime = currentTime,
                Round = match.Round,
                Participant1Name = match.Participant1Name,
                Participant2Name = match.Participant2Name
            });

            currentTime += timeBetweenMatches;
        }

        var schedule = new TournamentSchedule
        {
            TournamentId = tournamentId,
            ScheduledMatches = scheduledMatches
        };

        _schedules[tournamentId] = schedule;
        return Task.FromResult(Result<TournamentSchedule>.Success(schedule));
    }

    /// <inheritdoc />
    public Task<Result<TournamentSchedule>> GetScheduleAsync(string tournamentId, CancellationToken ct = default)
    {
        if (_schedules.TryGetValue(tournamentId, out var schedule))
        {
            return Task.FromResult(Result<TournamentSchedule>.Success(schedule));
        }
        return Task.FromResult(Result<TournamentSchedule>.Failure("Schedule not found", ErrorType.NotFound));
    }

    /// <inheritdoc />
    public Task<Result<TournamentMatch>> UpdateMatchScheduleAsync(string tournamentId, string matchId, DateTime scheduledTime, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<TournamentMatch>.Failure("Tournament not found", ErrorType.NotFound));

        var match = tournament.Matches.FirstOrDefault(m => m.Id == matchId);
        if (match == null)
            return Task.FromResult(Result<TournamentMatch>.Failure("Match not found", ErrorType.NotFound));

        var updatedMatch = match with { ScheduledTime = scheduledTime };
        var updatedMatches = tournament.Matches.Select(m => m.Id == matchId ? updatedMatch : m).ToList();
        var updatedTournament = tournament with { Matches = updatedMatches };
        _tournaments[tournamentId] = updatedTournament;

        return Task.FromResult(Result<TournamentMatch>.Success(updatedMatch));
    }

    #endregion

    #region Prize Pool

    /// <inheritdoc />
    public Task<Result<PrizePool>> ContributeToPrizePoolAsync(string tournamentId, string contributorId, decimal amount, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<PrizePool>.Failure("Tournament not found", ErrorType.NotFound));

        if (amount <= 0)
            return Task.FromResult(Result<PrizePool>.Failure("Contribution must be positive", ErrorType.Validation));

        var currentPrizePool = tournament.PrizePool ?? new PrizePool { Currency = "USD", Allocations = Array.Empty<PrizeAllocation>(), Contributors = Array.Empty<PrizeContributor>() };

        var contributor = new PrizeContributor
        {
            UserId = contributorId,
            DisplayName = contributorId, // Would be populated from user service
            Amount = amount
        };

        var updatedContributors = currentPrizePool.Contributors.ToList();
        updatedContributors.Add(contributor);

        var updatedPrizePool = currentPrizePool with
        {
            TotalAmount = currentPrizePool.TotalAmount + amount,
            Contributors = updatedContributors
        };

        var updatedTournament = tournament with { PrizePool = updatedPrizePool };
        _tournaments[tournamentId] = updatedTournament;

        return Task.FromResult(Result<PrizePool>.Success(updatedPrizePool));
    }

    /// <inheritdoc />
    public Task<Result> DistributePrizesAsync(string tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result.Failure("Tournament not found", ErrorType.NotFound));

        if (tournament.Status != TournamentStatus.Completed)
            return Task.FromResult(Result.Failure("Tournament must be completed before distributing prizes", ErrorType.Validation));

        if (tournament.PrizePool == null || tournament.PrizePool.TotalAmount == 0)
            return Task.FromResult(Result.Success()); // Nothing to distribute

        var prizePool = tournament.PrizePool;
        var allocations = new List<PrizeAllocation>();

        // Standard distribution: 50%, 30%, 20% for top 3
        if (prizePool.DistributionType == PrizeDistributionType.Standard)
        {
            var firstPlace = prizePool.TotalAmount * 0.5m;
            var secondPlace = prizePool.TotalAmount * 0.3m;
            var thirdPlace = prizePool.TotalAmount * 0.2m;

            allocations.Add(new PrizeAllocation { Place = 1, Amount = firstPlace });
            allocations.Add(new PrizeAllocation { Place = 2, Amount = secondPlace });
            allocations.Add(new PrizeAllocation { Place = 3, Amount = thirdPlace });
        }

        var updatedPrizePool = prizePool with { Allocations = allocations };
        var updatedTournament = tournament with { PrizePool = updatedPrizePool };
        _tournaments[tournamentId] = updatedTournament;

        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Standings

    /// <inheritdoc />
    public Task<Result<TournamentStandings>> GetStandingsAsync(string tournamentId, CancellationToken ct = default)
    {
        if (_standings.TryGetValue(tournamentId, out var cachedStandings))
        {
            return Task.FromResult(Result<TournamentStandings>.Success(cachedStandings));
        }

        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<TournamentStandings>.Failure("Tournament not found", ErrorType.NotFound));

        // Calculate standings from matches
        var entries = new List<StandingEntry>();

        foreach (var participant in tournament.Participants)
        {
            var participantMatches = tournament.Matches.Where(m =>
                m.Participant1Id == participant.Id || m.Participant2Id == participant.Id).ToList();

            int wins = 0, losses = 0, draws = 0, points = 0;

            foreach (var match in participantMatches.Where(m => m.Status == MatchStatus.Completed))
            {
                if (match.WinnerId == participant.Id)
                {
                    wins++;
                    points += 3;
                }
                else if (match.LoserId == participant.Id)
                {
                    losses++;
                }
                else
                {
                    draws++;
                    points += 1;
                }
            }

            entries.Add(new StandingEntry
            {
                Position = 0, // Will be set after sorting
                ParticipantId = participant.Id,
                DisplayName = participant.DisplayName,
                Wins = wins,
                Losses = losses,
                Draws = draws,
                Points = points
            });
        }

        // Sort by points, then wins
        entries = entries
            .OrderByDescending(e => e.Points)
            .ThenByDescending(e => e.Wins)
            .ThenBy(e => e.Losses)
            .ToList();

        // Assign positions
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i] = entries[i] with { Position = i + 1 };
        }

        var standings = new TournamentStandings
        {
            TournamentId = tournamentId,
            Entries = entries
        };

        _standings[tournamentId] = standings;
        return Task.FromResult(Result<TournamentStandings>.Success(standings));
    }

    #endregion

    #region Statistics

    /// <inheritdoc />
    public Task<Result<Dictionary<string, object>>> GetTournamentStatsAsync(string tournamentId, CancellationToken ct = default)
    {
        if (!_tournaments.TryGetValue(tournamentId, out var tournament))
            return Task.FromResult(Result<Dictionary<string, object>>.Failure("Tournament not found", ErrorType.NotFound));

        var stats = new Dictionary<string, object>
        {
            ["TotalParticipants"] = tournament.CurrentParticipants,
            ["TotalMatches"] = tournament.Matches.Count,
            ["CompletedMatches"] = tournament.Matches.Count(m => m.Status == MatchStatus.Completed),
            ["RemainingMatches"] = tournament.Matches.Count(m => m.Status != MatchStatus.Completed && m.Status != MatchStatus.Bye),
            ["CheckInRate"] = tournament.CurrentParticipants > 0
                ? (double)tournament.Participants.Count(p => p.Status == ParticipantStatus.CheckedIn) / tournament.CurrentParticipants
                : 0.0,
            ["PrizePool"] = tournament.PrizePool?.TotalAmount ?? 0m,
            ["Duration"] = tournament.TournamentEnd.HasValue
                ? tournament.TournamentEnd.Value - tournament.TournamentStart
                : TimeSpan.Zero
        };

        return Task.FromResult(Result<Dictionary<string, object>>.Success(stats));
    }

    #endregion
}
