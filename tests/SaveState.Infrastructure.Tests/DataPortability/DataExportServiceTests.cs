using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Infrastructure.DataPortability;
using SaveState.Infrastructure.Persistence;
using System.Text.Json;
using Xunit;

namespace SaveState.Infrastructure.Tests.DataPortability;

public class DataExportServiceTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<ICompletionPredictionService> _predictionServiceMock;
    private readonly Mock<ILogger<DataExportService>> _loggerMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly DataExportService _service;
    private readonly SaveStateDbContext _dbContext; // Needed for constructor but not used in ExportGameLibraryAsync

    public DataExportServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _predictionServiceMock = new Mock<ICompletionPredictionService>();
        _loggerMock = new Mock<ILogger<DataExportService>>();
        _timeProviderMock = new Mock<ITimeProvider>();

        var options = new DbContextOptionsBuilder<SaveStateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new SaveStateDbContext(options);

        _service = new DataExportService(
            _gameRepositoryMock.Object,
            _dbContext,
            _predictionServiceMock.Object,
            _loggerMock.Object,
            _timeProviderMock.Object);
    }

    [Fact]
    public async Task ExportGameLibraryAsync_IncludesPredictions()
    {
        // Arrange
        var exportedAt = new DateTime(2026, 2, 19, 15, 30, 0, DateTimeKind.Utc);
        _timeProviderMock.SetupGet(tp => tp.UtcNow).Returns(exportedAt);

        var gameId = Guid.NewGuid();
        var game = Game.Create("Test Game", Guid.NewGuid());

        // Set ID using reflection (since Create generates a new Guid)
        typeof(Game).GetProperty(nameof(Game.Id)).SetValue(game, gameId);

        // Set Status to Running: First set install path (Installed), then MarkAsRunning
        game.SetInstallPath("C:\\FakeInstallPath");
        game.MarkAsRunning();

        // Set TotalPlayTime to 5 hours (needs reflection as setter is private and no public method exists in snippet)
        typeof(Game).GetProperty(nameof(Game.TotalPlayTime)).SetValue(game, TimeSpan.FromHours(5));

        _gameRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Game> { game });

        var prediction = new GameCompletionPrediction(
            GameId.From(gameId),
            "Test Game",
            TimeSpan.FromHours(10),
            85.0,
            "HLTB",
            new List<string> { "Main Story" }
        );

        _predictionServiceMock.Setup(x => x.GetPredictionForGameAsync(It.IsAny<GameId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(prediction));

        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            var result = await _service.ExportGameLibraryAsync(tempFile);

            // Assert
            result.IsSuccess.Should().BeTrue();
            File.Exists(tempFile).Should().BeTrue();

            var json = await File.ReadAllTextAsync(tempFile);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("totalGames").GetInt32().Should().Be(1);
            root.GetProperty("exportedAt").GetDateTime().Should().Be(exportedAt);
            var gamesArray = root.GetProperty("games");
            var gameJson = gamesArray[0];

            gameJson.GetProperty("title").GetString().Should().Be("Test Game");

            // Allow capital or camel case depending on settings
            var hasPrediction = gameJson.TryGetProperty("completionPrediction", out var predictionJson) ||
                                gameJson.TryGetProperty("CompletionPrediction", out predictionJson);

            hasPrediction.Should().BeTrue();
            predictionJson.ValueKind.Should().Be(JsonValueKind.Object);

            // Check property names inside prediction
            // Depending on anonymous object serialization, it should be camelCase because of JsonOptions
            predictionJson.GetProperty("estimatedRemainingHours").GetDouble().Should().Be(10.0);
            predictionJson.GetProperty("confidenceScore").GetDouble().Should().Be(85.0);

            // Factors was explicitly mapped
            predictionJson.GetProperty("factors").GetArrayLength().Should().Be(1);
            predictionJson.GetProperty("factors")[0].GetString().Should().Be("Main Story");

            _predictionServiceMock.Verify(x => x.GetPredictionForGameAsync(It.IsAny<GameId>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
