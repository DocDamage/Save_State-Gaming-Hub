
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;
using SaveState.Infrastructure.Repositories;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.Repositories;

public class GameMediaRepositoryTests : IDisposable
{
    private readonly SaveStateDbContext _context;
    private readonly GameMediaRepository _repository;

    public GameMediaRepositoryTests()
    {
        var options = SaveStateDbContextModelFactory.CreateInMemoryOptions<SaveStateDbContext>();

        _context = new SaveStateDbContext(options);
        _repository = new GameMediaRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddMedia()
    {
        // Arrange
        var gameId = GameId.From(Guid.NewGuid());
        var userId = UserId.From(Guid.NewGuid());
        var media = GameMedia.Create(gameId, userId, MediaType.Screenshot, "path/to/img.png", 1024, "png");

        // Act
        await _repository.AddAsync(media);

        // Assert
        var retrieved = await _context.GameMedia.FirstOrDefaultAsync(m => m.Id == media.Id);
        retrieved.Should().NotBeNull();
        retrieved!.FilePath.Should().Be("path/to/img.png");
    }

    [Fact]
    public async Task GetByGameIdAsync_ShouldReturnMediaForGame()
    {
        // Arrange
        var gameId = GameId.From(Guid.NewGuid());
        var userId = UserId.From(Guid.NewGuid());
        var media1 = GameMedia.Create(gameId, userId, MediaType.Screenshot, "path1", 100, "png");
        var media2 = GameMedia.Create(gameId, userId, MediaType.Video, "path2", 200, "mp4");
        var otherGameMedia = GameMedia.Create(GameId.From(Guid.NewGuid()), userId, MediaType.Screenshot, "path3", 100, "png");

        await _repository.AddAsync(media1);
        await _repository.AddAsync(media2);
        await _repository.AddAsync(otherGameMedia);

        // Act
        var result = await _repository.GetByGameIdAsync(gameId, userId);

        // Assert
        result.Should().HaveCount(2);
    }
}
