using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Social.Entities;
using SaveState.Core.Social.Repositories;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

/// <summary>
/// Implementation of ICommunityRepository using EF Core.
/// </summary>
public class CommunityRepository : ICommunityRepository
{
    private readonly SaveStateDbContext _context;

    public CommunityRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Challenge>> GetChallengeByIdAsync(Guid id, CancellationToken ct = default)
    {
        var challenge = await _context.Challenges
            .Include(c => c.Requirements)
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return challenge != null
            ? Result.Success(challenge)
            : Result.Failure<Challenge>("Challenge not found.");
    }

    public async Task<Result<IReadOnlyList<Challenge>>> GetActiveChallengesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var challenges = await _context.Challenges
            .Where(c => c.StartDate <= now && c.EndDate >= now && !c.IsDeleted)
            .Include(c => c.Requirements)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<Challenge>>(challenges);
    }

    public async Task<Result<Guid>> CreateChallengeAsync(Challenge challenge, CancellationToken ct = default)
    {
        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync(ct);
        return Result.Success(challenge.Id);
    }

    public async Task<Result> UpdateChallengeAsync(Challenge challenge, CancellationToken ct = default)
    {
        _context.Challenges.Update(challenge);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> JoinChallengeAsync(Guid challengeId, Guid userId, CancellationToken ct = default)
    {
        var challenge = await _context.Challenges
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null) return Result.Failure("Challenge not found.");

        if (challenge.Participants.Any(p => p.UserId == userId))
            return Result.Failure("User already participating in this challenge.");

        challenge.Participants.Add(new ChallengeParticipant
        {
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            Progress = 0,
            IsCompleted = false
        });

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UpdateChallengeProgressAsync(Guid challengeId, Guid userId, double progress, CancellationToken ct = default)
    {
        var challenge = await _context.Challenges
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == challengeId, ct);

        if (challenge == null) return Result.Failure("Challenge not found.");

        var participant = challenge.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null) return Result.Failure("User not participating in this challenge.");

        participant.Progress = progress;
        if (progress >= 100 && !participant.IsCompleted)
        {
            participant.IsCompleted = true;
            participant.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<Leaderboard>> GetLeaderboardAsync(Guid id, CancellationToken ct = default)
    {
        var leaderboard = await _context.Leaderboards
            .Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        return leaderboard != null
            ? Result.Success(leaderboard)
            : Result.Failure<Leaderboard>("Leaderboard not found.");
    }

    public async Task<Result<Leaderboard>> GetLeaderboardByCategoryAsync(LeaderboardCategory category, CancellationToken ct = default)
    {
        var leaderboard = await _context.Leaderboards
            .Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.Category == category && !l.IsDeleted, ct);

        return leaderboard != null
            ? Result.Success(leaderboard)
            : Result.Failure<Leaderboard>("Leaderboard not found.");
    }

    public async Task<Result<Guid>> CreateLeaderboardAsync(Leaderboard leaderboard, CancellationToken ct = default)
    {
        _context.Leaderboards.Add(leaderboard);
        await _context.SaveChangesAsync(ct);
        return Result.Success(leaderboard.Id);
    }

    public async Task<Result> UpdateLeaderboardAsync(Leaderboard leaderboard, CancellationToken ct = default)
    {
        _context.Leaderboards.Update(leaderboard);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UpdateLeaderboardEntryAsync(LeaderboardRanking entry, CancellationToken ct = default)
    {
        var leaderboard = await _context.Leaderboards
            .Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.Id == entry.LeaderboardId, ct);

        if (leaderboard == null) return Result.Failure("Leaderboard not found.");

        var existingEntry = leaderboard.Entries.FirstOrDefault(e => e.UserId == entry.UserId);
        if (existingEntry != null)
        {
            existingEntry.Score = entry.Score;
            existingEntry.LastUpdated = DateTime.UtcNow;
            existingEntry.Metadata = entry.Metadata;
        }
        else
        {
            leaderboard.Entries.Add(entry);
        }

        // Re-rank entries
        var sortedEntries = leaderboard.Entries.OrderByDescending(e => e.Score).ToList();
        for (int i = 0; i < sortedEntries.Count; i++)
        {
            sortedEntries[i].Rank = i + 1;
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}

