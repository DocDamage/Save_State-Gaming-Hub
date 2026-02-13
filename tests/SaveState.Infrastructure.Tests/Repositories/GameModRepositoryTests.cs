
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;
using SaveState.Infrastructure.Repositories;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.Repositories;

public class GameModRepositoryTests : IDisposable
{
    private readonly SaveStateDbContext _context;
    private readonly GameModRepository _repository;

    public GameModRepositoryTests()
    {
        var options = SaveStateDbContextModelFactory.CreateInMemoryOptions<SaveStateDbContext>();

        _context = new SaveStateDbContext(options);
        _repository = new GameModRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddMod()
    {
        // Arrange
        var gameId = GameId.From(Guid.NewGuid());
        var mod = GameMod.Create(gameId, "Test Mod", "1.0", "C:/Mods", 1000);

        // Act
        await _repository.AddAsync(mod);

        // Assert
        var retrieved = await _context.GameMods.FirstOrDefaultAsync(m => m.Id == mod.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Test Mod");
    }

    [Fact]
    public async Task GetByGameIdAsync_ShouldReturnModsForGame()
    {
        // Arrange
        var gameId = GameId.From(Guid.NewGuid());
        var mod1 = GameMod.Create(gameId, "Mod 1", "1.0", "path1", 100);
        var mod2 = GameMod.Create(gameId, "Mod 2", "1.0", "path2", 100);
        var otherGameMod = GameMod.Create(GameId.From(Guid.NewGuid()), "Other", "1.0", "path", 100);

        await _repository.AddAsync(mod1);
        await _repository.AddAsync(mod2);
        await _repository.AddAsync(otherGameMod);

        // Act
        var result = await _repository.GetByGameIdAsync(gameId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(m => m.Name == "Mod 1");
        result.Should().Contain(m => m.Name == "Mod 2");
    }
}
