
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;
using SaveState.Infrastructure.Repositories;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.Repositories;

public class GameNoteRepositoryTests : IDisposable
{
    private readonly SaveStateDbContext _context;
    private readonly GameNoteRepository _repository;
    private readonly Mock<ITimeProvider> _timeProviderMock;

    public GameNoteRepositoryTests()
    {
        var options = SaveStateDbContextModelFactory.CreateInMemoryOptions<SaveStateDbContext>();

        _context = new SaveStateDbContext(options);
        _repository = new GameNoteRepository(_context);
        _timeProviderMock = new Mock<ITimeProvider>();
        _timeProviderMock.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddNote()
    {
        // Arrange
        var gameId = GameId.From(Guid.NewGuid());
        var userId = UserId.From(Guid.NewGuid());
        var note = GameNote.Create(gameId, userId, "Test Note", "Test Content", _timeProviderMock.Object);

        // Act
        await _repository.AddAsync(note);

        // Assert
        var retrieved = await _context.GameNotes.FirstOrDefaultAsync(n => n.Id == note.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Test Note");
    }

    [Fact]
    public async Task GetByGameIdAsync_ShouldReturnNotesForGame()
    {
        // Arrange
        var gameId = GameId.From(Guid.NewGuid());
        var userId = UserId.From(Guid.NewGuid());
        var note1 = GameNote.Create(gameId, userId, "Note 1", "Content 1", _timeProviderMock.Object);
        var note2 = GameNote.Create(gameId, userId, "Note 2", "Content 2", _timeProviderMock.Object);
        var otherGameNote = GameNote.Create(GameId.From(Guid.NewGuid()), userId, "Other", "Content", _timeProviderMock.Object);

        await _repository.AddAsync(note1);
        await _repository.AddAsync(note2);
        await _repository.AddAsync(otherGameNote);

        // Act
        var result = await _repository.GetByGameIdAsync(gameId, userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(n => n.Title == "Note 1");
        result.Should().Contain(n => n.Title == "Note 2");
    }
}
