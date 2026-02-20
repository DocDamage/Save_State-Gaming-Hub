using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Social;
using SaveState.Core.Social.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for friends and their activities.
/// </summary>
public class FriendRepository : IFriendRepository
{
    private readonly SaveStateDbContext _context;
    private readonly ITimeProvider _timeProvider;

    public FriendRepository(SaveStateDbContext context, ITimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    // Friend operations
    public async Task<Friend?> GetByPlatformIdAsync(SocialPlatform platform, string platformUserId, CancellationToken ct = default)
    {
        return await _context.Friends
            .FirstOrDefaultAsync(f => f.Platform == platform && f.PlatformUserId == platformUserId, ct)
            .ConfigureAwait(false);
    }

    public async Task<PagedResult<Friend>> GetFriendsAsync(
        SocialPlatform? platform = null,
        bool? isOnline = null,
        CancellationToken ct = default)
    {
        var query = _context.Friends.AsQueryable();

        if (platform.HasValue)
        {
            query = query.Where(f => f.Platform == platform.Value);
        }

        if (isOnline.HasValue)
        {
            query = query.Where(f => f.IsOnline == isOnline.Value);
        }

        // Order by online status (online first), then by name
        query = query.OrderByDescending(f => f.IsOnline).ThenBy(f => f.Name);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query.ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<Friend>(items, totalCount, 1, totalCount);
    }

    public async Task<Friend> AddOrUpdateFriendAsync(Friend friend, CancellationToken ct = default)
    {
        var existing = await GetByPlatformIdAsync(friend.Platform, friend.PlatformUserId, ct);
        if (existing is not null)
        {
            // Update existing friend
            existing.UpdateProfile(friend.Name, friend.AvatarUrl);
            existing.UpdateStatus(friend.IsOnline, friend.CurrentGame);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
            return existing;
        }
        else
        {
            // Add new friend
            await _context.Friends.AddAsync(friend, ct).ConfigureAwait(false);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
            return friend;
        }
    }

    public async Task UpdateFriendStatusAsync(Guid friendId, bool isOnline, string? currentGame, CancellationToken ct = default)
    {
        var friend = await _context.Friends.FindAsync(new object[] { friendId }, ct).ConfigureAwait(false);
        if (friend is not null)
        {
            friend.UpdateStatus(isOnline, currentGame);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task DeleteFriendAsync(Guid friendId, CancellationToken ct = default)
    {
        var friend = await _context.Friends.FindAsync(new object[] { friendId }, ct).ConfigureAwait(false);
        if (friend is not null)
        {
            _context.Friends.Remove(friend);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    // Activity operations
    public async Task<PagedResult<FriendActivity>> GetActivitiesAsync(
        int limit = 50,
        SocialPlatform? platform = null,
        ActivityType? activityType = null,
        CancellationToken ct = default)
    {
        var query = _context.FriendActivities
            .Include(a => a.Friend)
            .AsQueryable();

        if (platform.HasValue)
        {
            query = query.Where(a => a.Platform == platform.Value);
        }

        if (activityType.HasValue)
        {
            query = query.Where(a => a.Type == activityType.Value);
        }

        // Order by timestamp (newest first)
        query = query.OrderByDescending(a => a.Timestamp);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<FriendActivity>(items, totalCount, 1, limit);
    }

    public async Task<IReadOnlyList<FriendActivity>> GetFriendActivitiesAsync(
        Guid friendId,
        int limit = 20,
        CancellationToken ct = default)
    {
        return await _context.FriendActivities
            .Where(a => a.FriendId == friendId)
            .Include(a => a.Friend)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task AddActivityAsync(FriendActivity activity, CancellationToken ct = default)
    {
        await _context.FriendActivities.AddAsync(activity, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<int> CleanupOldActivitiesAsync(int daysToKeep = 30, CancellationToken ct = default)
    {
        var cutoffDate = _timeProvider.UtcNow.AddDays(-daysToKeep);
        var oldActivities = await _context.FriendActivities
            .Where(a => a.Timestamp < cutoffDate)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (oldActivities.Any())
        {
            _context.FriendActivities.RemoveRange(oldActivities);
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return oldActivities.Count;
    }

    public async Task<FriendActivityStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var friends = await _context.Friends.ToListAsync(ct).ConfigureAwait(false);
        var activities = await _context.FriendActivities.ToListAsync(ct).ConfigureAwait(false);

        var activitiesByType = activities
            .GroupBy(a => a.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        var friendsByPlatform = friends
            .GroupBy(f => f.Platform)
            .ToDictionary(g => g.Key, g => g.Count());

        return new FriendActivityStatistics(
            TotalFriends: friends.Count,
            OnlineFriends: friends.Count(f => f.IsOnline),
            TotalActivities: activities.Count,
            ActivitiesByType: activitiesByType,
            FriendsByPlatform: friendsByPlatform);
    }
}