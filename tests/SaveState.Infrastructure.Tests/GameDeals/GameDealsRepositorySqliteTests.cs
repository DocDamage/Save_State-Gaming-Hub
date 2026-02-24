using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common.Services;
using SaveState.Core.GameDeals;
using SaveState.Infrastructure.GameDeals;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Tests.GameDeals;

public sealed class GameDealsRepositorySqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SaveStateDbContext _context;
    private readonly GameDealsRepository _repository;

    public GameDealsRepositorySqliteTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SaveStateDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new SaveStateDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new GameDealsRepository(_context, SystemTimeProvider.Instance);
    }

    [Fact]
    public async Task GetDealsAsync_WithMinDiscountPercent_ReturnsOnlyActiveMatchingDeals()
    {
        var now = SystemTimeProvider.Instance.UtcNow;
        var store = new GameStore { Id = "steam", Name = "Steam" };

        _context.Set<GameStore>().Add(store);
        _context.GameDeals.AddRange(
            new GameDeal
            {
                Title = "Active High Discount",
                CurrentPrice = 40m,
                RegularPrice = 100m,
                Store = store,
                DealEnd = now.AddDays(2),
                LastUpdated = now
            },
            new GameDeal
            {
                Title = "Active Low Discount",
                CurrentPrice = 95m,
                RegularPrice = 100m,
                Store = store,
                DealEnd = now.AddDays(2),
                LastUpdated = now
            },
            new GameDeal
            {
                Title = "Expired High Discount",
                CurrentPrice = 10m,
                RegularPrice = 100m,
                Store = store,
                DealEnd = now.AddDays(-1),
                LastUpdated = now
            });

        await _context.SaveChangesAsync();

        var results = await _repository.GetDealsAsync(
            new DealFilterOptions
            {
                MinDiscountPercent = 20,
                SortOrder = DealSortOrder.DiscountPercent
            });

        results.Should().ContainSingle();
        results[0].Title.Should().Be("Active High Discount");
    }

    [Fact]
    public async Task GetBestDealAsync_ExcludesExpiredDeals()
    {
        var now = SystemTimeProvider.Instance.UtcNow;
        var store = new GameStore { Id = "gog", Name = "GOG" };

        _context.Set<GameStore>().Add(store);
        _context.GameDeals.AddRange(
            new GameDeal
            {
                Title = "Halo Collection",
                CurrentPrice = 25m,
                RegularPrice = 50m,
                Store = store,
                DealEnd = now.AddDays(1),
                LastUpdated = now
            },
            new GameDeal
            {
                Title = "Halo Collection",
                CurrentPrice = 5m,
                RegularPrice = 50m,
                Store = store,
                DealEnd = now.AddDays(-1),
                LastUpdated = now
            });

        await _context.SaveChangesAsync();

        var bestDeal = await _repository.GetBestDealAsync("Halo Collection");

        bestDeal.Should().NotBeNull();
        bestDeal!.CurrentPrice.Should().Be(25m);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
