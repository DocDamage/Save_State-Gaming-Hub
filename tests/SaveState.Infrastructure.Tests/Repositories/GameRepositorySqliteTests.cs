using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Performance;
using SaveState.Infrastructure.Persistence;
using SaveState.Infrastructure.Repositories;

namespace SaveState.Infrastructure.Tests.Repositories;

public sealed class GameRepositorySqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SaveStateDbContext _context;
    private readonly GameRepository _repository;

    public GameRepositorySqliteTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SaveStateDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new SaveStateDbContext(options);
        _context.Database.EnsureCreated();

        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var optimizer = new QueryOptimizer(cache, NullLogger<QueryOptimizer>.Instance);
        var metrics = new Mock<IApplicationMetrics>();

        _repository = new GameRepository(_context, metrics.Object, optimizer, SystemTimeProvider.Instance);
    }

    [Fact]
    public async Task GetPlatformStatisticsAsync_ReturnsCountsByPlatform()
    {
        var pc = new Platform(
            PlatformName.From("PC"),
            PlatformShortName.From("PC"),
            PlatformType.Computer);

        var snes = new Platform(
            PlatformName.From("SNES"),
            PlatformShortName.From("SNES"),
            PlatformType.Console);

        _context.Platforms.AddRange(pc, snes);
        _context.Games.AddRange(
            Game.Create("Game 1", pc.Id),
            Game.Create("Game 2", pc.Id),
            Game.Create("Game 3", snes.Id),
            Game.Create("Game Without Platform", null));

        await _context.SaveChangesAsync();

        var stats = await _repository.GetPlatformStatisticsAsync();

        stats.Should().ContainKey("PC").WhoseValue.Should().Be(2);
        stats.Should().ContainKey("SNES").WhoseValue.Should().Be(1);
        stats.Should().NotContainKey("Game Without Platform");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
