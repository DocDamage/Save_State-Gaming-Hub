namespace SaveState.Infrastructure.Mugen;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Implementation of the MUGEN tournament service.
/// Manages tournament creation, brackets, and match results.
/// </summary>
public class MugenTournamentService : IMugenTournamentService
{
    private readonly SaveState.Core.Mugen.IMugenCharacterRepository _characterRepository;
    private readonly IMugenStatsService _statsService;
    private readonly SaveState.Core.Mugen.IMugenTournamentRepository _tournamentRepository;

    public MugenTournamentService(
        SaveState.Core.Mugen.IMugenCharacterRepository characterRepository,
        IMugenStatsService statsService,
        SaveState.Core.Mugen.IMugenTournamentRepository tournamentRepository)
    {
        _characterRepository = characterRepository;
        _statsService = statsService;
        _tournamentRepository = tournamentRepository;
    }

    public async Task<Result<MugenTournament>> CreateTournamentAsync(
        CreateTournamentRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // Validate participants exist
            var participants = new List<MugenCharacter>();
            foreach (var participantId in request.ParticipantIds)
            {
                var characterResult = await _characterRepository.GetByIdAsync(participantId, ct);
                if (characterResult.IsFailure || characterResult.Value is null)
                    return Result.Failure<MugenTournament>($"Participant {participantId} not found");

                participants.Add(characterResult.Value);
            }

            if (participants.Count < 2)
                return Result.Failure<MugenTournament>("Tournament must have at least 2 participants");

            // Create tournament
            var tournament = MugenTournament.Create(request.Name, request.Format);

            // Persist tournament to database
            await _tournamentRepository.AddAsync(tournament, ct);

            return Result.Success<MugenTournament>(tournament);
        }
        catch (Exception ex)
        {
            return Result.Failure<MugenTournament>($"Failed to create tournament: {ex.Message}");
        }
    }

    public async Task<Result> RecordMatchResultAsync(
        Guid tournamentId,
        Guid matchId,
        MatchResult result,
        CancellationToken ct = default)
    {
        try
        {
            // Load tournament with matches
            var tournamentResult = await _tournamentRepository.GetByIdAsync(tournamentId, ct);
            if (tournamentResult.IsFailure || tournamentResult.Value is null)
                return Result.Failure("Tournament not found");
            var tournament = tournamentResult.Value;

            // Find the match
            var match = tournament.Matches.FirstOrDefault(m => m.Id == matchId);
            if (match is null)
                return Result.Failure("Match not found in tournament");

            // Update match result
            var winnerId = result == MatchResult.Player1Win ? match.Player1CharacterId :
                          result == MatchResult.Player2Win ? match.Player2CharacterId : null;

            if (winnerId.HasValue)
            {
                match.Complete(winnerId.Value);
            }

            // Save changes
            await _tournamentRepository.UpdateAsync(tournament, ct);

            // Winner advancement to next round
            TournamentBracketManager.AdvanceWinner(tournament, match);

            // Save again after advancement
            await _tournamentRepository.UpdateAsync(tournament, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to record match result: {ex.Message}");
        }
    }

    public async Task<Result<TournamentBracket>> GetBracketAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            // Load tournament from database
            var tournamentResult = await _tournamentRepository.GetByIdAsync(tournamentId, ct);
            if (tournamentResult.IsFailure || tournamentResult.Value is null)
                return Result.Failure<TournamentBracket>("Tournament not found");
            var tournament = tournamentResult.Value;

            // Group matches by round and build bracket
            var rounds = tournament.Matches
                .GroupBy(m => m.Round)
                .OrderBy(g => g.Key)
                .Select(g => new TournamentRound(
                    g.Key,
                    GetRoundName(g.Key, tournament.Format),
                    g.OrderBy(m => m.MatchNumber)
                     .Select(m => new TournamentMatch(
                         m.Id,
                         m.Player1CharacterId,
                         m.Player2CharacterId,
                         m.Status == MatchStatus.Completed ?
                             (m.WinnerId == m.Player1CharacterId ? MatchResult.Player1Win :
                              m.WinnerId == m.Player2CharacterId ? MatchResult.Player2Win : MatchResult.Draw) : null,
                         m.Status == MatchStatus.Completed))
                     .ToArray()))
                .ToList();

            var bracket = new TournamentBracket(tournamentId, rounds);
            return Result.Success<TournamentBracket>(bracket);
        }
        catch (Exception ex)
        {
            return Result.Failure<TournamentBracket>($"Failed to get bracket: {ex.Message}");
        }
    }

    private string GetRoundName(int round, TournamentFormat format)
    {
        return format switch
        {
            TournamentFormat.SingleElimination => round switch
            {
                1 => "Round of 16",
                2 => "Quarter-Finals",
                3 => "Semi-Finals",
                4 => "Finals",
                _ => $"Round {round}"
            },
            TournamentFormat.DoubleElimination => round switch
            {
                1 => "Round 1",
                2 => "Round 2",
                3 => "Winners Semi-Finals",
                4 => "Losers Round 1",
                5 => "Winners Finals",
                6 => "Losers Semi-Finals",
                7 => "Losers Finals",
                8 => "Grand Finals",
                _ => $"Round {round}"
            },
            TournamentFormat.RoundRobin => $"Round {round}",
            _ => $"Round {round}"
        };
    }

    public async Task<Result> AddParticipantAsync(
        Guid tournamentId,
        Guid characterId,
        int? seed = null,
        CancellationToken ct = default)
    {
        try
        {
            // Load tournament
            var tournamentResult = await _tournamentRepository.GetByIdAsync(tournamentId, ct);
            if (tournamentResult.IsFailure || tournamentResult.Value is null)
                return Result.Failure("Tournament not found");
            var tournament = tournamentResult.Value;

            // Check if tournament is still in setup phase
            if (tournament.Status != TournamentStatus.Setup)
                return Result.Failure("Cannot add participants to a tournament that has already started");

            // Validate character exists
            var characterResult = await _characterRepository.GetByIdAsync(characterId, ct);
            if (characterResult.IsFailure)
                return Result.Failure("Character not found");

            // Check if character is already a participant
            if (tournament.Participants.Any(p => p.CharacterId == characterId))
                return Result.Failure("Character is already a participant in this tournament");

            // Add participant
            var participantSeed = seed ?? (tournament.Participants.Count + 1);
            var participant = TournamentParticipant.Create(tournamentId, characterId, participantSeed);
            tournament.Participants.Add(participant);

            // Save changes
            await _tournamentRepository.UpdateAsync(tournament, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to add participant: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<TournamentStanding>>> GetStandingsAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            // Load tournament with participants and matches
            var tournamentResult = await _tournamentRepository.GetByIdAsync(tournamentId, ct);
            if (tournamentResult.IsFailure || tournamentResult.Value is null)
                return Result.Failure<IReadOnlyList<TournamentStanding>>("Tournament not found");
            var tournament = tournamentResult.Value;

            // Calculate standings based on tournament progress
            var standings = new List<TournamentStanding>();

            foreach (var participant in tournament.Participants)
            {
                var wins = tournament.Matches
                    .Where(m => m.Status == MatchStatus.Completed && m.WinnerId == participant.CharacterId)
                    .Count();

                var losses = tournament.Matches
                    .Where(m => m.Status == MatchStatus.Completed &&
                               (m.Player1CharacterId == participant.CharacterId || m.Player2CharacterId == participant.CharacterId) &&
                               m.WinnerId != participant.CharacterId)
                    .Count();

                // Calculate current round based on tournament format and progress
                var currentRound = CalculateCurrentRound(participant, tournament);

                var isEliminated = participant.Status == ParticipantStatus.Eliminated;

                standings.Add(new TournamentStanding(
                    participant.CharacterId,
                    participant.Character.Name, // Assuming Character is loaded
                    wins,
                    losses,
                    wins * 3, // 3 points per win
                    currentRound,
                    isEliminated));
            }

            // Sort by score, then by wins
            standings = standings
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.Wins)
                .ThenBy(s => s.Losses)
                .ToList();

            return Result.Success<IReadOnlyList<TournamentStanding>>(standings);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<TournamentStanding>>($"Failed to get standings: {ex.Message}");
        }
    }

    private int CalculateCurrentRound(TournamentParticipant participant, MugenTournament tournament)
    {
        if (participant.Status == ParticipantStatus.Winner)
            return 999; // Final round

        if (participant.Status == ParticipantStatus.Eliminated)
            return 0; // Eliminated

        // For active participants, determine current round based on completed matches
        var participantMatches = tournament.Matches
            .Where(m => m.Player1CharacterId == participant.CharacterId || m.Player2CharacterId == participant.CharacterId)
            .OrderByDescending(m => m.Round)
            .ToList();

        var lastCompletedMatch = participantMatches.FirstOrDefault(m => m.Status == MatchStatus.Completed);
        if (lastCompletedMatch != null)
        {
            return lastCompletedMatch.Round + 1; // Next round
        }

        // If no completed matches, they're in round 1
        return 1;
    }

    public async Task<Result> StartTournamentAsync(Guid tournamentId, CancellationToken ct = default)
    {
        try
        {
            var tournamentResult = await _tournamentRepository.GetByIdAsync(tournamentId, ct);
            if (tournamentResult.IsFailure || tournamentResult.Value is null)
                return Result.Failure("Tournament not found");

            var tournament = tournamentResult.Value;
            if (tournament.Status != TournamentStatus.Setup)
                return Result.Failure("Tournament is already started or completed.");

            if (tournament.Participants.Count < 2)
                return Result.Failure("Tournament must have at least 2 participants to start.");

            // Generate initial matches
            List<TournamentMatchEntity> matches;
            if (tournament.Format == TournamentFormat.SingleElimination)
            {
                matches = TournamentBracketManager.GenerateSingleEliminationMatches(tournamentId, tournament.Participants.ToList());
            }
            else
            {
                return Result.Failure($"Tournament format {tournament.Format} is not yet supported for auto-generation.");
            }

            foreach (var match in matches)
            {
                tournament.Matches.Add(match);
            }

            tournament.Start();
            await _tournamentRepository.UpdateAsync(tournament, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to start tournament: {ex.Message}");
        }
    }
}

