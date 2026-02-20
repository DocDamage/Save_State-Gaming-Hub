using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.GameLibrary.Services;
using Xunit;

namespace SaveState.Infrastructure.Tests.GameLibrary;

public class MemoryServicesTests
{
    private readonly Mock<ILogger<GameMemoryReader>> _memoryReaderLoggerMock;
    private readonly Mock<ILogger<MemoryPatternDatabase>> _patternDbLoggerMock;

    public MemoryServicesTests()
    {
        _memoryReaderLoggerMock = new Mock<ILogger<GameMemoryReader>>();
        _patternDbLoggerMock = new Mock<ILogger<MemoryPatternDatabase>>();
    }

    [Fact]
    public void MemoryPatternDatabase_GetSignaturesForGame_KnownGame_ReturnsSignatures()
    {
        // Arrange
        var database = new MemoryPatternDatabase(_patternDbLoggerMock.Object);

        // Act
        var result = database.GetSignaturesForGame("Celeste");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().Contain(sig => sig.Name == "Strawberries");
        result.Value.Should().Contain(sig => sig.Name == "Deaths");
        result.Value.Should().Contain(sig => sig.Name == "Chapter");
    }

    [Fact]
    public void MemoryPatternDatabase_GetSignaturesForGame_UnknownGame_ReturnsUniversalPatterns()
    {
        // Arrange
        var database = new MemoryPatternDatabase(_patternDbLoggerMock.Object);

        // Act
        var result = database.GetSignaturesForGame("UnknownGame");

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should return universal patterns (*) even for unknown games
        result.Value.Should().NotBeEmpty();
        result.Value.Should().Contain(sig => sig.GameTitle == "*");
    }

    [Fact]
    public void MemoryPatternDatabase_GetSignaturesForGame_SimilarGame_ReturnsSignatures()
    {
        // Arrange
        var database = new MemoryPatternDatabase(_patternDbLoggerMock.Object);

        // Act
        var result = database.GetSignaturesForGame("Celeste Demo"); // Similar to "Celeste"

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        // Should find Celeste signatures due to fuzzy matching
    }

    [Fact]
    public void MemoryPatternDatabase_GetSupportedGames_ReturnsKnownGames()
    {
        // Arrange
        var database = new MemoryPatternDatabase(_patternDbLoggerMock.Object);

        // Act
        var result = database.GetSupportedGames();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Celeste");
        result.Value.Should().Contain("Hollow Knight");
        result.Value.Should().Contain("Stardew Valley");
        result.Value.Should().Contain("Hades");
        result.Value.Should().Contain("Dead Cells");
        result.Value.Should().Contain("Risk of Rain 2");
        result.Value.Should().Contain("Slay the Spire");
        result.Value.Should().Contain("Cuphead");
        result.Value.Should().Contain("Shovel Knight");
        result.Value.Should().Contain("Ori and the Blind Forest");
        result.Value.Should().Contain("*");
    }

    [Fact]
    public void MemoryPatternDatabase_AddSignature_IncreasesSupportedGames()
    {
        // Arrange
        var database = new MemoryPatternDatabase(_patternDbLoggerMock.Object);
        var initialCount = database.GetSupportedGames().Value.Count;

        var newSignature = new GameMemorySignature
        {
            Name = "Test Pattern",
            Pattern = "AABBCC",
            Offset = 0,
            ValueType = "int32",
            Description = "Test signature"
        };

        // Act
        var addResult = database.AddSignature("TestGame", newSignature);
        var finalGames = database.GetSupportedGames();

        // Assert
        addResult.IsSuccess.Should().BeTrue();
        finalGames.Value.Should().Contain("TestGame");
        finalGames.Value.Count.Should().BeGreaterThan(initialCount);
    }

    [Fact]
    public async Task GameMemoryReader_DetachAsync_WhenNotAttached_Succeeds()
    {
        // Arrange
        var patternDb = new MemoryPatternDatabase(_patternDbLoggerMock.Object);
        var memoryReader = new GameMemoryReader(_memoryReaderLoggerMock.Object, patternDb);

        // Ensure not attached
        memoryReader.IsAttached.Should().BeFalse();

        // Act
        var result = await memoryReader.DetachAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GameMemoryReader_DetectPatternsAsync_WhenNotAttached_Fails()
    {
        // Arrange
        var patternDb = new MemoryPatternDatabase(_patternDbLoggerMock.Object);
        var memoryReader = new GameMemoryReader(_memoryReaderLoggerMock.Object, patternDb);

        // Act
        var result = await memoryReader.DetectPatternsAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Not attached to any process");
    }

    [Fact]
    public void GameMemoryReader_IsAttached_InitiallyFalse()
    {
        // Arrange
        var patternDb = new MemoryPatternDatabase(_patternDbLoggerMock.Object);
        var memoryReader = new GameMemoryReader(_memoryReaderLoggerMock.Object, patternDb);

        // Assert
        memoryReader.IsAttached.Should().BeFalse();
    }

    // Note: AttachToProcessAsync and memory reading tests would require integration testing
    // with actual processes, which is better suited for integration tests due to Windows-specific APIs
}