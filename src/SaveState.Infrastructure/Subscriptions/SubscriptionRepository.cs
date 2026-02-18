// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Subscriptions;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Subscriptions;

/// <summary>
/// Repository for subscription data persistence.
/// </summary>
public sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<SubscriptionRepository> _logger;

    public SubscriptionRepository(
        SaveStateDbContext dbContext,
        ILogger<SubscriptionRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSubscriptionEntity>> GetUserSubscriptionsAsync(
        Guid userId, 
        CancellationToken ct = default)
    {
        return await _dbContext.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.ServiceName)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<UserSubscriptionEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<UserSubscriptionEntity?> GetByServiceIdAsync(
        Guid userId, 
        string serviceId, 
        CancellationToken ct = default)
    {
        return await _dbContext.UserSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.UserId == userId && s.ServiceId == serviceId, 
                ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(UserSubscriptionEntity subscription, CancellationToken ct = default)
    {
        subscription.CreatedAt = DateTime.UtcNow;
        _dbContext.UserSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation(
            "Added subscription {ServiceId} for user {UserId}", 
            subscription.ServiceId, 
            subscription.UserId);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(UserSubscriptionEntity subscription, CancellationToken ct = default)
    {
        subscription.UpdatedAt = DateTime.UtcNow;
        _dbContext.UserSubscriptions.Update(subscription);
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation(
            "Updated subscription {ServiceId} for user {UserId}", 
            subscription.ServiceId, 
            subscription.UserId);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var subscription = await _dbContext.UserSubscriptions.FindAsync(new object[] { id }, ct);
        if (subscription is not null)
        {
            _dbContext.UserSubscriptions.Remove(subscription);
            await _dbContext.SaveChangesAsync(ct);
            
            _logger.LogInformation(
                "Deleted subscription {ServiceId} for user {UserId}", 
                subscription.ServiceId, 
                subscription.UserId);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrackedGameEntity>> GetTrackedGamesAsync(
        Guid userId, 
        CancellationToken ct = default)
    {
        return await _dbContext.TrackedSubscriptionGames
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.TrackedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddTrackedGameAsync(TrackedGameEntity game, CancellationToken ct = default)
    {
        // Check if already tracked
        var existing = await _dbContext.TrackedSubscriptionGames
            .FirstOrDefaultAsync(
                g => g.UserId == game.UserId && g.GameTitle == game.GameTitle, 
                ct);
        
        if (existing is not null)
        {
            _logger.LogDebug(
                "Game {GameTitle} already tracked for user {UserId}", 
                game.GameTitle, 
                game.UserId);
            return;
        }

        game.TrackedAt = DateTime.UtcNow;
        _dbContext.TrackedSubscriptionGames.Add(game);
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation(
            "Added tracked game {GameTitle} for user {UserId}", 
            game.GameTitle, 
            game.UserId);
    }

    /// <inheritdoc />
    public async Task RemoveTrackedGameAsync(Guid userId, string gameTitle, CancellationToken ct = default)
    {
        var game = await _dbContext.TrackedSubscriptionGames
            .FirstOrDefaultAsync(
                g => g.UserId == userId && g.GameTitle == gameTitle, 
                ct);
        
        if (game is not null)
        {
            _dbContext.TrackedSubscriptionGames.Remove(game);
            await _dbContext.SaveChangesAsync(ct);
            
            _logger.LogInformation(
                "Removed tracked game {GameTitle} for user {UserId}", 
                gameTitle, 
                userId);
        }
    }

    /// <inheritdoc />
    public async Task SaveGamesAsync(IEnumerable<SubscriptionGame> games, CancellationToken ct = default)
    {
        // In a real implementation, this would save games to the database
        _logger.LogInformation("Saving {Count} games to subscription cache", games.Count());
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SubscriptionGame>> GetCachedGamesAsync(CancellationToken ct = default)
    {
        // Return cached games from database
        return await Task.FromResult<IReadOnlyList<SubscriptionGame>>(new List<SubscriptionGame>());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SubscriptionGame>> GetGamesByServiceAsync(SubscriptionServiceType serviceType, CancellationToken ct = default)
    {
        // Return games filtered by service type
        return await Task.FromResult<IReadOnlyList<SubscriptionGame>>(new List<SubscriptionGame>());
    }

    /// <inheritdoc />
    public async Task ClearCacheAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Clearing subscription game cache");
        await Task.CompletedTask;
    }
}
