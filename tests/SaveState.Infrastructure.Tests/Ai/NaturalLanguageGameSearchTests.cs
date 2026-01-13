using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Infrastructure.Ai.Services;
using SaveState.Core.GameLibrary.Entities;
using Xunit;

namespace SaveState.Infrastructure.Tests.Ai;

public class NaturalLanguageGameSearchTests
{
    private readonly Mock<IAiOrchestrator> _aiOrchestratorMock;
    private readonly Mock<ILogger<NaturalLanguageGameSearch>> _loggerMock;
    private readonly NaturalLanguageGameSearch _searchService;

    public NaturalLanguageGameSearchTests()
    {
        _aiOrchestratorMock = new Mock<IAiOrchestrator>();
        _loggerMock = new Mock<ILogger<NaturalLanguageGameSearch>>();
        _searchService = new NaturalLanguageGameSearch(_aiOrchestratorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ParseQueryAsync_ShouldReturnCorrectFilter_WhenAiProcessingSucceeds()
    {
        // Arrange
        string query = "RPGs from the 90s";
        string jsonResponse = "{\"Genre\": \"Role-playing (RPG)\", \"MinReleaseYear\": 1990, \"MaxReleaseYear\": 1999}";
        _aiOrchestratorMock.Setup(x => x.GenerateTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(jsonResponse));

        // Act
        var result = await _searchService.ParseQueryAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Genre.Should().Be("Role-playing (RPG)");
        result.MinReleaseYear.Should().Be(1990);
        result.MaxReleaseYear.Should().Be(1999);
    }

    [Fact]
    public async Task ParseQueryAsync_ShouldReturnEmptyFilter_WhenAiProcessingFails()
    {
        // Arrange
        string query = "Something invalid";
        _aiOrchestratorMock.Setup(x => x.GenerateTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<string>("AI Error"));

        // Act
        var result = await _searchService.ParseQueryAsync(query);

        // Assert
        result.Should().BeEquivalentTo(new CollectionFilter());
    }

    [Fact]
    public async Task ParseQueryAsync_ShouldHandleMarkDownCodeBlocks()
    {
        // Arrange
        string query = "Platformers";
        string jsonResponse = "```json\n{\"Genre\": \"Platform\"}\n```";
        _aiOrchestratorMock.Setup(x => x.GenerateTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(jsonResponse));

        // Act
        var result = await _searchService.ParseQueryAsync(query);

        // Assert
        result.Genre.Should().Be("Platform");
    }
}
