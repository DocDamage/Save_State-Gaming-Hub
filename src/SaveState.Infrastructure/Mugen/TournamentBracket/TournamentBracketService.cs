using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Core.Mugen.TournamentEvents.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.TournamentEvents.Services;

/// <summary>
/// Service for managing tournament brackets.
/// </summary>
internal class TournamentEventServiceOperations : ITournamentEventService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<TournamentEventService> _logger;

    public TournamentEventServiceOperations(
        SaveStateDbContext dbContext,
        ILogger<TournamentEventService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<TournamentEvent>> CreateTournamentAsync(
        CreateTournamentRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = new TournamentEvent
            {
                Name = request.Name,
                Description = request.Description,
                Format = request.Format,
                MaxParticipants = request.MaxParticipants,
                ScheduledStart = request.ScheduledStart,
                Organizer = request.Organizer,
                Rules = request.Rules,
                Settings = request.Settings,
                IsPublic = request.IsPublic,
                Tags = request.Tags ?? new List<string>(),
                Status = TournamentStatus.RegistrationOpen,
                Participants = new List<TournamentParticipant>(),
                Matches = new List<TournamentMatch>(),
                Rounds = new List<TournamentRound>()
            };

            _dbContext.TournamentEvents.Add(tournament);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Created tournament {TournamentId} - {Name}", tournament.Id, tournament.Name);
            return Result<TournamentEvent>.Success(tournament);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tournament");
            return Result<TournamentEvent>.Failure($"Failed to create tournament: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<TournamentEvent>> GetTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result<TournamentEvent>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            return Result<TournamentEvent>.Success(tournament);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tournament {TournamentId}", tournamentId);
            return Result<TournamentEvent>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<TournamentEvent>> UpdateTournamentAsync(
        Guid tournamentId,
        CreateTournamentRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result<TournamentEvent>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            tournament.Name = request.Name;
            tournament.Description = request.Description;
            tournament.Format = request.Format;
            tournament.ScheduledStart = request.ScheduledStart;
            tournament.Rules = request.Rules;
            tournament.Settings = request.Settings;
            tournament.IsPublic = request.IsPublic;
            tournament.Tags = request.Tags ?? new List<string>();

            await _dbContext.SaveChangesAsync(ct);
            return Result<TournamentEvent>.Success(tournament);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tournament");
            return Result<TournamentEvent>.Failure($"Update failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> DeleteTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            _dbContext.TournamentEvents.Remove(tournament);
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete tournament");
            return Result.Failure($"Delete failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<TournamentEvent>>> SearchTournamentsAsync(
        TournamentFilter filter,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.TournamentEvents.AsNoTracking().AsQueryable();

            if (filter.Format.HasValue)
                query = query.Where(t => t.Format == filter.Format.Value);

            if (filter.Status.HasValue)
                query = query.Where(t => t.Status == filter.Status.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(t => t.ScheduledStart >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(t => t.ScheduledStart <= filter.ToDate.Value);

            if (!string.IsNullOrEmpty(filter.Organizer))
                query = query.Where(t => t.Organizer == filter.Organizer);

            if (filter.IsPublic.HasValue)
                query = query.Where(t => t.IsPublic == filter.IsPublic.Value);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(term));
            }

            var tournaments = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Result<List<TournamentEvent>>.Success(tournaments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search tournaments");
            return Result<List<TournamentEvent>>.Failure($"Search failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<TournamentParticipant>> RegisterParticipantAsync(
        Guid tournamentId,
        RegisterParticipantRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result<TournamentParticipant>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            if (tournament.Participants.Count >= tournament.MaxParticipants)
                return Result<TournamentParticipant>.Failure("Tournament is full", ErrorType.Validation);

            var participant = new TournamentParticipant
            {
                TournamentId = tournamentId,
                Name = request.Name,
                UserId = request.UserId,
                ContactInfo = request.ContactInfo,
                Country = request.Country,
                Team = request.Team,
                Character = request.Character,
                StreamUrl = request.StreamUrl,
                Seed = tournament.Participants.Count + 1,
                InitialSeed = tournament.Participants.Count + 1
            };

            tournament.Participants.Add(participant);
            await _dbContext.SaveChangesAsync(ct);

            return Result<TournamentParticipant>.Success(participant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register participant");
            return Result<TournamentParticipant>.Failure($"Registration failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> UnregisterParticipantAsync(
        Guid tournamentId,
        Guid participantId,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            var participant = tournament.Participants.FirstOrDefault(p => p.Id == participantId);
            if (participant == null)
                return Result.Failure($"Participant {participantId} not found", ErrorType.NotFound);

            tournament.Participants.Remove(participant);
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister participant");
            return Result.Failure($"Unregister failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> CheckInParticipantAsync(
        Guid tournamentId,
        Guid participantId,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            var participant = tournament.Participants.FirstOrDefault(p => p.Id == participantId);
            if (participant == null)
                return Result.Failure($"Participant {participantId} not found", ErrorType.NotFound);

            participant.IsCheckedIn = true;
            participant.CheckedInAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check in participant");
            return Result.Failure($"Check-in failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<TournamentEvent>> StartTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result<TournamentEvent>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            tournament.Status = TournamentStatus.InProgress;
            tournament.StartedAt = DateTime.UtcNow;
            tournament.CurrentRound = 1;

            await _dbContext.SaveChangesAsync(ct);

            return Result<TournamentEvent>.Success(tournament);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start tournament");
            return Result<TournamentEvent>.Failure($"Start failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<TournamentEvent>> GenerateBracketAsync(
        Guid tournamentId,
        SeedingMethod seedingMethod,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result<TournamentEvent>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            // Apply seeding
            var participants = seedingMethod switch
            {
                SeedingMethod.Random => tournament.Participants.OrderBy(_ => Guid.NewGuid()).ToList(),
                SeedingMethod.RegistrationOrder => tournament.Participants.OrderBy(p => p.RegisteredAt).ToList(),
                _ => tournament.Participants.ToList()
            };

            // Generate matches based on format
            var matches = GenerateSingleEliminationMatches(participants, tournamentId);
            tournament.Matches = matches;

            // Create rounds
            var rounds = CreateRounds(matches);
            tournament.Rounds = rounds;

            await _dbContext.SaveChangesAsync(ct);

            return Result<TournamentEvent>.Success(tournament);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate bracket");
            return Result<TournamentEvent>.Failure($"Generation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<TournamentMatch>> GetMatchAsync(
        Guid matchId,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Matches.Any(m => m.Id == matchId), ct);

            if (tournament == null)
                return Result<TournamentMatch>.Failure($"Match {matchId} not found", ErrorType.NotFound);

            var match = tournament.Matches.First(m => m.Id == matchId);
            return Result<TournamentMatch>.Success(match);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get match");
            return Result<TournamentMatch>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<TournamentMatch>> ReportMatchResultAsync(
        Guid matchId,
        ReportMatchResultRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Matches.Any(m => m.Id == matchId), ct);

            if (tournament == null)
                return Result<TournamentMatch>.Failure($"Match {matchId} not found", ErrorType.NotFound);

            var match = tournament.Matches.First(m => m.Id == matchId);

            match.Result = new MatchResult
            {
                Score1 = request.Score1,
                Score2 = request.Score2,
                WinnerId = request.WinnerId,
                RoundResults = request.RoundResults ?? new List<RoundResult>(),
                EndCondition = request.EndCondition,
                ReplayPath = request.ReplayPath
            };

            match.Status = MatchStatus.Completed;
            match.EndedAt = DateTime.UtcNow;

            var winner = tournament.Participants.First(p => p.Id == request.WinnerId);
            var loser = match.Participant1?.Id == request.WinnerId ? match.Participant2 : match.Participant1;

            match.Winner = winner;
            match.Loser = loser;

            winner.Statistics.MatchesWon++;
            if (loser != null)
            {
                loser.Statistics.MatchesLost++;
                loser.IsEliminated = true;
                loser.EliminatedInRound = match.Round;
            }

            await _dbContext.SaveChangesAsync(ct);

            return Result<TournamentMatch>.Success(match);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report match result");
            return Result<TournamentMatch>.Failure($"Report failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result> AdvanceWinnerAsync(
        Guid matchId,
        CancellationToken ct = default)
    {
        // Auto-advance handled by bracket generation
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<List<TournamentMatch>>> GetTournamentMatchesAsync(
        Guid tournamentId,
        int? round = null,
        BracketPosition? bracket = null,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result<List<TournamentMatch>>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            var matches = tournament.Matches.AsEnumerable();

            if (round.HasValue)
                matches = matches.Where(m => m.Round == round.Value);

            if (bracket.HasValue)
                matches = matches.Where(m => m.BracketPosition == bracket.Value);

            return Result<List<TournamentMatch>>.Success(matches.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tournament matches");
            return Result<List<TournamentMatch>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<TournamentMatch>>> GetParticipantMatchesAsync(
        Guid tournamentId,
        Guid participantId,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result<List<TournamentMatch>>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            var matches = tournament.Matches
                .Where(m => m.Participant1?.Id == participantId || m.Participant2?.Id == participantId)
                .ToList();

            return Result<List<TournamentMatch>>.Success(matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get participant matches");
            return Result<List<TournamentMatch>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<List<TournamentMatch>>> GetUpcomingMatchesAsync(
        Guid tournamentId,
        int count = 5,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<List<TournamentMatch>>.Success(new List<TournamentMatch>()));
    }

    public async Task<Result<TournamentMatch>> ScheduleMatchAsync(
        Guid matchId,
        DateTime scheduledTime,
        string? station = null,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Matches.Any(m => m.Id == matchId), ct);

            if (tournament == null)
                return Result<TournamentMatch>.Failure($"Match {matchId} not found", ErrorType.NotFound);

            var match = tournament.Matches.First(m => m.Id == matchId);
            match.ScheduledTime = scheduledTime;
            match.Station = station;

            await _dbContext.SaveChangesAsync(ct);

            return Result<TournamentMatch>.Success(match);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule match");
            return Result<TournamentMatch>.Failure($"Schedule failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<TournamentMatch>> AssignStationAsync(
        Guid matchId,
        string station,
        bool isStreamed = false,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Matches.Any(m => m.Id == matchId), ct);

            if (tournament == null)
                return Result<TournamentMatch>.Failure($"Match {matchId} not found", ErrorType.NotFound);

            var match = tournament.Matches.First(m => m.Id == matchId);
            match.Station = station;
            match.IsStreamed = isStreamed;

            await _dbContext.SaveChangesAsync(ct);

            return Result<TournamentMatch>.Success(match);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign station");
            return Result<TournamentMatch>.Failure($"Assign failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<List<TournamentParticipant>>> GetStandingsAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<List<TournamentParticipant>>.Success(new List<TournamentParticipant>()));
    }

    public Task<Result> SeedParticipantsAsync(
        Guid tournamentId,
        List<Guid> participantIdsInOrder,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> RandomizeSeedsAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<List<TournamentParticipant>>> GetTop8Async(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result<List<TournamentParticipant>>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            var top8 = tournament.Participants
                .Where(p => p.Placement.HasValue && p.Placement.Value <= 8)
                .OrderBy(p => p.Placement)
                .ToList();

            return Result<List<TournamentParticipant>>.Success(top8);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top 8");
            return Result<List<TournamentParticipant>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<string>> ExportToChallongeAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success("{}"));
    }

    public Task<Result<string>> GenerateStreamOverlayAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<string>.Success("<html></html>"));
    }

    public Task<Result<StreamOverlayData>> GetStreamOverlayDataAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<StreamOverlayData>.Success(new StreamOverlayData()));
    }

    public Task<Result> SendDiscordNotificationAsync(
        Guid tournamentId,
        string message,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public async Task<Result> PauseTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            tournament.Status = TournamentStatus.Paused;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause tournament");
            return Result.Failure($"Pause failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> ResumeTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            tournament.Status = TournamentStatus.InProgress;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume tournament");
            return Result.Failure($"Resume failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<TournamentEvent>> EndTournamentAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        try
        {
            var tournament = await _dbContext.TournamentEvents
                .FirstOrDefaultAsync(t => t.Id == tournamentId, ct);

            if (tournament == null)
                return Result<TournamentEvent>.Failure($"Tournament {tournamentId} not found", ErrorType.NotFound);

            tournament.Status = TournamentStatus.Completed;
            tournament.EndedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            return Result<TournamentEvent>.Success(tournament);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end tournament");
            return Result<TournamentEvent>.Failure($"End failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<TournamentStatistics>> GetStatisticsAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<TournamentStatistics>.Success(new TournamentStatistics()));
    }

    public Task<Result<List<string>>> ValidateBracketAsync(
        Guid tournamentId,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<List<string>>.Success(new List<string>()));
    }

    public Task<Result<TournamentMatch>> ResetMatchAsync(
        Guid matchId,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<TournamentMatch>.Failure("Not implemented", ErrorType.NotImplemented));
    }

    public Task<Result> DisqualifyParticipantAsync(
        Guid tournamentId,
        Guid participantId,
        string reason,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<TournamentParticipant>> ReplaceParticipantAsync(
        Guid tournamentId,
        Guid participantIdToReplace,
        RegisterParticipantRequest newParticipant,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<TournamentParticipant>.Failure("Not implemented", ErrorType.NotImplemented));
    }

    // Helper methods

    private static List<TournamentMatch> GenerateSingleEliminationMatches(
        List<TournamentParticipant> participants,
        Guid tournamentId)
    {
        var matches = new List<TournamentMatch>();
        var participantCount = participants.Count;
        
        // Calculate number of rounds
        var rounds = (int)Math.Ceiling(Math.Log2(participantCount));
        var bracketSize = (int)Math.Pow(2, rounds);
        
        // Add byes if needed
        while (participants.Count < bracketSize)
        {
            participants.Add(null!);
        }

        var matchNumber = 1;
        var previousRoundMatches = new List<TournamentMatch>();

        // Generate first round matches
        for (int i = 0; i < participants.Count; i += 2)
        {
            var match = new TournamentMatch
            {
                TournamentId = tournamentId,
                Round = 1,
                MatchNumber = matchNumber++,
                BracketPosition = BracketPosition.Winners,
                Participant1 = participants[i],
                Participant2 = participants[i + 1],
                MatchIdentifier = $"W-R1-M{matchNumber - 1}"
            };

            if (participants[i] == null || participants[i + 1] == null)
            {
                match.Status = MatchStatus.Bye;
                match.Winner = participants[i] ?? participants[i + 1];
            }

            matches.Add(match);
            previousRoundMatches.Add(match);
        }

        // Generate subsequent rounds
        for (int round = 2; round <= rounds; round++)
        {
            var currentRoundMatches = new List<TournamentMatch>();
            var matchesInRound = previousRoundMatches.Count / 2;

            for (int i = 0; i < matchesInRound; i++)
            {
                var match = new TournamentMatch
                {
                    TournamentId = tournamentId,
                    Round = round,
                    MatchNumber = matchNumber++,
                    BracketPosition = BracketPosition.Winners,
                    MatchIdentifier = $"W-R{round}-M{i + 1}"
                };

                // Link to previous matches
                var prevMatch1 = previousRoundMatches[i * 2];
                var prevMatch2 = previousRoundMatches[i * 2 + 1];
                
                match.PreviousMatchIds.Add(prevMatch1.Id);
                match.PreviousMatchIds.Add(prevMatch2.Id);
                
                prevMatch1.NextMatchForWinnerId = match.Id;
                prevMatch2.NextMatchForWinnerId = match.Id;

                matches.Add(match);
                currentRoundMatches.Add(match);
            }

            previousRoundMatches = currentRoundMatches;
        }

        return matches;
    }

    private static List<TournamentRound> CreateRounds(List<TournamentMatch> matches)
    {
        return matches
            .GroupBy(m => m.Round)
            .Select(g => new TournamentRound
            {
                RoundNumber = g.Key,
                Name = $"Round {g.Key}",
                BracketPosition = BracketPosition.Winners,
                MatchesCount = g.Count(),
                MatchIds = g.Select(m => m.Id).ToList()
            })
            .OrderBy(r => r.RoundNumber)
            .ToList();
    }
}

