using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Professional tournament management service for MUGEN.
/// Provides comprehensive tournament creation, bracket generation,
/// match scheduling, and competitive event management.
/// </summary>
public class MugenTournamentService : IMugenTournamentService
{
    private readonly ILogger<MugenTournamentService> _logger;
    private readonly IMugenTournamentRepository _tournamentRepository;
    private readonly ICacheService _cache;
    private readonly Dictionary<Guid, TournamentBracket> _activeBrackets = new();
    private readonly MugenTournamentServiceBracketGenerator _bracketGenerator;

    public MugenTournamentService(
        ILogger<MugenTournamentService> logger,
        ILoggerFactory loggerFactory,
        IMugenTournamentRepository tournamentRepository,
        ICacheService cache)
    {
        _logger = logger;
        _tournamentRepository = tournamentRepository;
        _cache = cache;
        _bracketGenerator = new MugenTournamentServiceBracketGenerator(loggerFactory.CreateLogger<MugenTournamentServiceBracketGenerator>());
    }

    public async Task<Result<MugenTournament>> CreateTournamentAsync(CreateTournamentRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating tournament '{Name}' with {Count} participants", request.Name, request.ParticipantIds.Count);

            // Validate request
            var validation = ValidateTournamentRequest(request);
            if (!validation.IsSuccess)
            {
                return Result.Failure<MugenTournament>(validation.Error!);
            }

            // Create tournament entity
            var tournament = MugenTournament.Create(request.Name, request.Format);

            // Add participants
            foreach (var participantId in request.ParticipantIds)
            {
                var participant = TournamentParticipant.Create(tournament.Id, participantId, 0);
                tournament.Participants.Add(participant);
            }

            // Save tournament
            await _tournamentRepository.AddAsync(tournament, ct);

            _logger.LogInformation("Created tournament {Id} with {Count} participants", tournament.Id, tournament.Participants.Count);
            return Result.Success<MugenTournament>(tournament);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tournament '{Name}'", request.Name);
            return Result.Failure<MugenTournament>($"Failed to create tournament: {ex.Message}");
        }
    }

    public async Task<Result<TournamentBracket>> GetBracketAsync(Guid tournamentId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            // Try cache first
            var cacheKey = $"bracket_{tournamentId}";
            if (_cache.TryGetValue<TournamentBracket>(cacheKey, out var cached) && cached != null)
            {
                return Result.Success<TournamentBracket>(cached);
            }

            // Check if we have it in memory
            if (_activeBrackets.TryGetValue(tournamentId, out var bracket))
            {
                _cache.Set(cacheKey, bracket, TimeSpan.FromHours(1));
                return Result.Success<TournamentBracket>(bracket);
            }

            // Get tournament and generate bracket
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, ct);
            if (!tournament.IsSuccess)
            {
                return Result.Failure<TournamentBracket>(tournament.Error!);
            }

            bracket = await GenerateBracketAsync(tournament.Value, ct);
            _activeBrackets[tournamentId] = bracket;

            // Cache the bracket
            _cache.Set(cacheKey, bracket, TimeSpan.FromHours(1));

            return Result.Success<TournamentBracket>(bracket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bracket for tournament {Id}", tournamentId);
            return Result.Failure<TournamentBracket>($"Failed to get bracket: {ex.Message}");
        }
    }

    public async Task<Result> AddParticipantAsync(Guid tournamentId, Guid characterId, int? seed = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Adding participant {CharacterId} to tournament {TournamentId}", characterId, tournamentId);

            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, ct);
            if (tournament == null)
            {
                return Result.Failure("Tournament not found");
            }

            if (tournament.Value.Status != TournamentStatus.Setup)
            {
                return Result.Failure("Cannot add participants to a tournament that has already started");
            }

            // Check if participant already exists
            if (tournament.Value.Participants.Any(p => p.CharacterId == characterId))
            {
                return Result.Failure("Participant is already in the tournament");
            }

            // Add participant
            var participant = TournamentParticipant.Create(tournamentId, characterId, seed ?? 0);

            tournament.Value.Participants.Add(participant);
            await _tournamentRepository.UpdateAsync(tournament.Value, ct);

            // Clear cached bracket
            var cacheKey = $"bracket_{tournamentId}";
            _cache.Remove(cacheKey);
            _activeBrackets.Remove(tournamentId);

            _logger.LogInformation("Added participant {CharacterId} to tournament {TournamentId}", characterId, tournamentId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding participant to tournament {TournamentId}", tournamentId);
            return Result.Failure($"Failed to add participant: {ex.Message}");
        }
    }

    public async Task<Result> RecordMatchResultAsync(Guid tournamentId, Guid matchId, MatchResult result, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Recording match result for tournament {TournamentId}, match {MatchId}: {Result}",
                tournamentId, matchId, result);

            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, ct);
            if (!tournament.IsSuccess)
            {
                return Result.Failure(tournament.Error!);
            }

            var match = tournament.Value.Matches.FirstOrDefault(m => m.Id == matchId);
            if (match == null)
            {
                return Result.Failure("Match not found in tournament");
            }

            // Update match result
            if (result == MatchResult.Player1Win && match.Player1CharacterId.HasValue)
            {
                match.Complete(match.Player1CharacterId.Value);
            }
            else if (result == MatchResult.Player2Win && match.Player2CharacterId.HasValue)
            {
                match.Complete(match.Player2CharacterId.Value);
            }

            await _tournamentRepository.UpdateAsync(tournament.Value, ct);

            // Check if tournament is complete
            await CheckTournamentCompletionAsync(tournament.Value, ct);

            // Update bracket cache
            _activeBrackets.Remove(tournamentId);
            var cacheKey = $"bracket_{tournamentId}";
            _cache.Remove(cacheKey);

            _logger.LogInformation("Recorded match result for tournament {TournamentId}", tournamentId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording match result for tournament {TournamentId}", tournamentId);
            return Result.Failure($"Failed to record match result: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<TournamentStanding>>> GetStandingsAsync(Guid tournamentId, CancellationToken ct = default)
    {
        try
        {
            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, ct);
            if (tournament == null)
            {
                return Result.Failure<IReadOnlyList<TournamentStanding>>("Tournament not found");
            }

            var standings = CalculateStandings(tournament.Value);
            return Result.Success<IReadOnlyList<TournamentStanding>>(standings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting standings for tournament {TournamentId}", tournamentId);
            return Result.Failure<IReadOnlyList<TournamentStanding>>($"Failed to get standings: {ex.Message}");
        }
    }

    public async Task<Result> StartTournamentAsync(Guid tournamentId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting tournament {TournamentId}", tournamentId);

            var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, ct);
            if (tournament == null)
            {
                return Result.Failure("Tournament not found");
            }

            if (tournament.Value.Status != TournamentStatus.Setup)
            {
                return Result.Failure("Tournament can only be started from setup state");
            }

            if (tournament.Value.Participants.Count < 2)
            {
                return Result.Failure("Tournament must have at least 2 participants");
            }

            // Generate initial bracket and matches
            var bracket = await GenerateBracketAsync(tournament.Value, ct);
            _activeBrackets[tournamentId] = bracket;

            // Mark tournament as in progress
            tournament.Value.Start();
            await _tournamentRepository.UpdateAsync(tournament.Value, ct);

            _logger.LogInformation("Started tournament {TournamentId} with {Count} participants",
                tournamentId, tournament.Value.Participants.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting tournament {TournamentId}", tournamentId);
            return Result.Failure($"Failed to start tournament: {ex.Message}");
        }
    }

    #region Private Methods

    private Result ValidateTournamentRequest(CreateTournamentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result.Failure("Tournament name cannot be empty");
        }

        if (request.ParticipantIds == null || request.ParticipantIds.Count < 2)
        {
            return Result.Failure("Tournament must have at least 2 participants");
        }

        if (request.ParticipantIds.Count > 128)
        {
            return Result.Failure("Tournament cannot have more than 128 participants");
        }

        // Check for duplicate participants
        if (request.ParticipantIds.Distinct().Count() != request.ParticipantIds.Count)
        {
            return Result.Failure("Tournament cannot have duplicate participants");
        }

        return Result.Success();
    }

    private async Task<TournamentBracket> GenerateBracketAsync(MugenTournament tournament, CancellationToken ct)
    {
        return await _bracketGenerator.GenerateBracketAsync(tournament, ct);
    }

    private async Task CheckTournamentCompletionAsync(MugenTournament tournament, CancellationToken ct)
    {
        // Check if all matches in the current round are complete
        var bracket = await GetBracketAsync(tournament.Id, ct);
        if (!bracket.IsSuccess)
        {
            return;
        }

        // Find the current round (the last round with incomplete matches)
        var currentRound = bracket.Value.Rounds.LastOrDefault(r => r.Matches.Any(m => !m.IsComplete));
        if (currentRound == null)
        {
            // All rounds are complete - tournament is finished
            var finalRound = bracket.Value.Rounds.LastOrDefault();
            if (finalRound != null)
            {
                var winnerMatch = finalRound.Matches.FirstOrDefault(m => m.IsComplete);
                if (winnerMatch != null)
                {
                    var winnerId = winnerMatch.Participant1Id ?? winnerMatch.Participant2Id;

                    if (winnerId != null)
                    {
                        tournament.Complete(winnerId.Value);
                        await _tournamentRepository.UpdateAsync(tournament, ct);

                        _logger.LogInformation("Tournament {Id} completed with winner {WinnerId}",
                            tournament.Id, winnerId);
                    }
                }
            }
        }
    }

    private IReadOnlyList<TournamentStanding> CalculateStandings(MugenTournament tournament)
    {
        var standings = new List<TournamentStanding>();

        foreach (var participant in tournament.Participants)
        {
            var wins = tournament.Matches.Count(m =>
                m.Status == MatchStatus.Completed &&
                ((m.Player1CharacterId == participant.CharacterId && m.WinnerId == m.Player1CharacterId) ||
                 (m.Player2CharacterId == participant.CharacterId && m.WinnerId == m.Player2CharacterId)));

            var losses = tournament.Matches.Count(m =>
                m.Status == MatchStatus.Completed &&
                ((m.Player1CharacterId == participant.CharacterId && m.WinnerId == m.Player2CharacterId) ||
                 (m.Player2CharacterId == participant.CharacterId && m.WinnerId == m.Player1CharacterId)));

            // Calculate points (wins = 3 points, draws = 1 point)
            var points = wins * 3;

            // Determine current round and elimination status
            var currentRound = 1;
            var isEliminated = false;

            if (tournament.Format == TournamentFormat.SingleElimination)
            {
                // In single elimination, participants are eliminated when they lose
                isEliminated = losses > 0;
                currentRound = isEliminated ? 0 : Math.Max(1, (int)Math.Log(tournament.Participants.Count, 2) - losses + 1);
            }

            standings.Add(new TournamentStanding(
                ParticipantId: participant.CharacterId,
                ParticipantName: $"Player_{participant.CharacterId.ToString().Substring(0, Math.Min(8, participant.CharacterId.ToString().Length))}",
                Wins: wins,
                Losses: losses,
                Points: points,
                CurrentRound: currentRound,
                IsEliminated: isEliminated
            ));
        }

        // Sort by points, then wins
        return standings
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.Wins)
            .ToList();
    }

    #endregion
}

/// <summary>
/// Advanced bracket generator for tournament management.
/// Handles different tournament formats and bracket generation algorithms.
/// </summary>
public class MugenTournamentServiceBracketGenerator
{
    private readonly ILogger<MugenTournamentServiceBracketGenerator> _logger;

    public MugenTournamentServiceBracketGenerator(ILogger<MugenTournamentServiceBracketGenerator> logger)
    {
        _logger = logger;
    }

    public async Task<TournamentBracket> GenerateBracketAsync(MugenTournament tournament, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating bracket for tournament {Id} ({Format}) with {Count} participants",
                tournament.Id, tournament.Format, tournament.Participants.Count);

            var rounds = tournament.Format switch
            {
                TournamentFormat.SingleElimination => GenerateSingleEliminationBracket(tournament),
                TournamentFormat.DoubleElimination => GenerateDoubleEliminationBracket(tournament),
                TournamentFormat.RoundRobin => GenerateRoundRobinBracket(tournament),
                _ => throw new ArgumentException($"Unsupported tournament format: {tournament.Format}")
            };

            return new TournamentBracket(tournament.Id, rounds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating bracket for tournament {Id}", tournament.Id);
            throw;
        }
    }

    private IReadOnlyList<TournamentRound> GenerateSingleEliminationBracket(MugenTournament tournament)
    {
        var participants = tournament.Participants
            .OrderBy(p => p.Seed)
            .ThenBy(p => Guid.NewGuid()) // Random tiebreaker
            .ToList();

        var rounds = new List<TournamentRound>();
        var participantCount = participants.Count;

        // Calculate number of rounds needed
        var roundCount = (int)Math.Ceiling(Math.Log(participantCount, 2));
        var currentParticipants = participants;

        for (int roundNum = 1; roundNum <= roundCount; roundNum++)
        {
            var roundName = GetRoundName(roundNum, roundCount);
            var matches = new List<TournamentMatch>();

            // Create matches for this round
            for (int i = 0; i < currentParticipants.Count; i += 2)
            {
                var participant1 = currentParticipants[i];
                var participant2 = i + 1 < currentParticipants.Count ? currentParticipants[i + 1] : null;

                var match = TournamentMatchEntity.Create(
                    tournamentId: tournament.Id,
                    round: roundNum,
                    matchNumber: i / 2 + 1,
                    player1CharacterId: participant1?.CharacterId,
                    player2CharacterId: participant2?.CharacterId
                );
                match.Start();

                tournament.Matches.Add(match);
                matches.Add(new TournamentMatch(match.Id, match.Player1CharacterId, match.Player2CharacterId, match.Status == MatchStatus.Completed ? (match.WinnerId == match.Player1CharacterId ? MatchResult.Player1Win : (match.WinnerId == match.Player2CharacterId ? MatchResult.Player2Win : null)) : null, match.Status == MatchStatus.Completed));
            }

            rounds.Add(new TournamentRound(roundNum, roundName, matches));

            // Prepare participants for next round (winners)
            currentParticipants = currentParticipants.Take(currentParticipants.Count / 2 + currentParticipants.Count % 2).ToList();
        }

        return rounds;
    }

    private IReadOnlyList<TournamentRound> GenerateDoubleEliminationBracket(MugenTournament tournament)
    {
        // Simplified double elimination - in practice this is much more complex
        // with winners and losers brackets
        var participants = tournament.Participants
            .OrderBy(p => p.Seed)
            .ToList();

        var rounds = new List<TournamentRound>();
        var participantCount = participants.Count;

        // For simplicity, treat as single elimination but note this is a placeholder
        _logger.LogWarning("Double elimination bracket generation is simplified");

        return GenerateSingleEliminationBracket(tournament);
    }

    private IReadOnlyList<TournamentRound> GenerateRoundRobinBracket(MugenTournament tournament)
    {
        var participants = tournament.Participants.ToList();
        var rounds = new List<TournamentRound>();

        // Round-robin: each participant plays every other participant once
        var roundCount = participants.Count - 1;
        var halfSize = participants.Count / 2;

        for (int roundNum = 1; roundNum <= roundCount; roundNum++)
        {
            var matches = new List<TournamentMatch>();

            // Create matches for this round using round-robin scheduling
            for (int i = 0; i < halfSize; i++)
            {
                var participant1 = participants[i];
                var participant2 = participants[participants.Count - 1 - i];

                var match = TournamentMatchEntity.Create(
                    tournamentId: tournament.Id,
                    round: roundNum,
                    matchNumber: i / 2 + 1,
                    player1CharacterId: participant1.CharacterId,
                    player2CharacterId: participant2.CharacterId
                );
                match.Start();

                tournament.Matches.Add(match);
                matches.Add(new TournamentMatch(match.Id, match.Player1CharacterId, match.Player2CharacterId, match.Status == MatchStatus.Completed ? (match.WinnerId == match.Player1CharacterId ? MatchResult.Player1Win : (match.WinnerId == match.Player2CharacterId ? MatchResult.Player2Win : null)) : null, match.Status == MatchStatus.Completed));
            }

            rounds.Add(new TournamentRound(roundNum, $"Round {roundNum}", matches));

            // Rotate participants for next round (keep first player fixed, rotate others)
            var first = participants[0];
            var rest = participants.Skip(1).ToList();
            rest.Add(rest[0]);
            rest.RemoveAt(0);
            participants = new[] { first }.Concat(rest).ToList();
        }

        return rounds;
    }

    private string GetRoundName(int roundNumber, int totalRounds)
    {
        return roundNumber switch
        {
            var r when r == totalRounds => "Finals",
            var r when r == totalRounds - 1 => "Semi-Finals",
            var r when r == totalRounds - 2 => "Quarter-Finals",
            var r when r >= totalRounds - 3 => $"Round {roundNumber}",
            _ => $"Preliminary Round {roundNumber}"
        };
    }
}
