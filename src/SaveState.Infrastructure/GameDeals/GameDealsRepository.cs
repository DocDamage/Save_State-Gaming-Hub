// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common.Services;
using SaveState.Core.GameDeals;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.GameDeals;

/// <summary>
/// Repository for game deal data persistence.
/// </summary>
public sealed class GameDealsRepository : IGameDealsRepository
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ITimeProvider _timeProvider;

    public GameDealsRepository(SaveStateDbContext dbContext, ITimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task SaveDealsAsync(IEnumerable<GameDeal> deals, CancellationToken ct = default)
    {
        foreach (var deal in deals)
        {
            var existing = await _dbContext.GameDeals
                .FirstOrDefaultAsync(d => d.Id == deal.Id, ct);

            if (existing == null)
            {
                _dbContext.GameDeals.Add(deal);
            }
            else
            {
                // Update existing
                existing.CurrentPrice = deal.CurrentPrice;
                existing.RegularPrice = deal.RegularPrice;
                existing.DealEnd = deal.DealEnd;
                existing.IsHistoricalLow = deal.IsHistoricalLow;
                existing.LastUpdated = deal.LastUpdated;
            }
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameDeal>> GetDealsAsync(DealFilterOptions? filter = null, CancellationToken ct = default)
    {
        var query = CreateActiveDealsQuery();

        if (filter?.StoreIds?.Any() == true)
        {
            var storeIds = filter.StoreIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            if (storeIds.Count > 0)
            {
                query = query.Where(d => storeIds.Contains(EF.Property<string?>(d, "StoreId")));
            }
        }

        if (filter?.MaxPrice.HasValue == true)
        {
            query = query.Where(d => d.CurrentPrice <= filter.MaxPrice.Value);
        }

        if (filter?.OnlyHistoricalLows == true)
        {
            query = query.Where(d => d.IsHistoricalLow);
        }

        var deals = await query.ToListAsync(ct);

        if (filter?.MinDiscountPercent.HasValue == true)
        {
            deals = deals
                .Where(d => d.DiscountPercent.HasValue && d.DiscountPercent.Value >= filter.MinDiscountPercent.Value)
                .ToList();
        }

        var orderedDeals = filter?.SortOrder switch
        {
            DealSortOrder.DiscountPercent => deals.OrderByDescending(d => d.DiscountPercent ?? decimal.MinValue),
            DealSortOrder.Price => deals.OrderBy(d => d.CurrentPrice),
            DealSortOrder.Title => deals.OrderBy(d => d.Title),
            DealSortOrder.Newest => deals.OrderByDescending(d => d.DealStart ?? DateTime.MinValue),
            _ => deals.OrderByDescending(d => d.DiscountPercent ?? decimal.MinValue)
        };

        return orderedDeals.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameDeal>> GetDealsForGameAsync(string gameTitle, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(gameTitle))
        {
            return Array.Empty<GameDeal>();
        }

        var normalizedTitle = gameTitle.Trim().ToLowerInvariant();
        var deals = await CreateActiveDealsQuery()
            .Where(d => d.Title.ToLower() == normalizedTitle)
            .ToListAsync(ct);

        return deals
            .OrderBy(d => d.CurrentPrice)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<GameDeal?> GetBestDealAsync(string gameTitle, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(gameTitle))
        {
            return null;
        }

        var normalizedTitle = gameTitle.Trim().ToLowerInvariant();
        var deals = await CreateActiveDealsQuery()
            .Where(d => d.Title.ToLower() == normalizedTitle)
            .ToListAsync(ct);

        return deals
            .OrderBy(d => d.CurrentPrice)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task SavePriceHistoryAsync(IEnumerable<PriceHistoryEntry> entries, CancellationToken ct = default)
    {
        _dbContext.PriceHistory.AddRange(entries);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceHistoryEntry>> GetPriceHistoryAsync(string gameTitle, string storeId, CancellationToken ct = default)
    {
        return await _dbContext.PriceHistory
            .AsNoTracking()
            .Where(h => h.GameTitle.ToLower() == gameTitle.ToLower() && h.StoreId == storeId)
            .OrderBy(h => h.Date)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task CreatePriceAlertAsync(PriceAlert alert, CancellationToken ct = default)
    {
        _dbContext.PriceAlerts.Add(alert);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceAlert>> GetPriceAlertsForUserAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.PriceAlerts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PriceAlert>> GetActivePriceAlertsAsync(CancellationToken ct = default)
    {
        return await _dbContext.PriceAlerts
            .AsNoTracking()
            .Where(a => a.IsActive)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task DeletePriceAlertAsync(Guid alertId, CancellationToken ct = default)
    {
        var alert = await _dbContext.PriceAlerts.FindAsync(new object[] { alertId }, ct);
        if (alert != null)
        {
            _dbContext.PriceAlerts.Remove(alert);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc />
    public async Task UpdateAlertLastTriggeredAsync(Guid alertId, CancellationToken ct = default)
    {
        var alert = await _dbContext.PriceAlerts.FindAsync(new object[] { alertId }, ct);
        if (alert != null)
        {
            alert.LastTriggeredAt = _timeProvider.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc />
    public async Task ClearOldDealsAsync(TimeSpan maxAge, CancellationToken ct = default)
    {
        var cutoff = _timeProvider.UtcNow - maxAge;
        var oldDeals = await _dbContext.GameDeals
            .Where(d => d.LastUpdated < cutoff)
            .ToListAsync(ct);

        _dbContext.GameDeals.RemoveRange(oldDeals);
        await _dbContext.SaveChangesAsync(ct);
    }

    private IQueryable<GameDeal> CreateActiveDealsQuery()
    {
        var now = _timeProvider.UtcNow;
        return _dbContext.GameDeals
            .AsNoTracking()
            .Where(d => d.DealEnd == null || d.DealEnd > now);
    }
}
