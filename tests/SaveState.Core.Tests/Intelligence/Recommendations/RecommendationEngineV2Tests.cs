using FluentAssertions;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Intelligence.Recommendations.Services;
using SaveState.Infrastructure.Intelligence.Recommendations;

namespace SaveState.Core.Tests.Intelligence.Recommendations;

public class RecommendationEngineV2Tests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IGameSessionRepository> _sessionRepositoryMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly HybridRecommendationEngineV2 _engine;

    public RecommendationEngineV2Tests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _sessionRepositoryMock = new Mock<IGameSessionRepository>();
        _timeProviderMock = new Mock<ITimeProvider>();

        _timeProviderMock.Setup(t => t.UtcNow).Returns(new DateTime(2026, 2, 13, 12, 0, 0));

        _engine = new HybridRecommendationEngineV2(
            _gameRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _timeProviderMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<HybridRecommendationEngineV2>>());
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithEmptyHistory_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _sessionRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession>());

        _gameRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Game>());

        // Act
        var result = await _engine.GetRecommendationsAsync(
            new RecommendationContext(userId, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithValidContext_ReturnsRecommendations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var genre = new Genre { Name = "RPG" };
        var game = new Game
        {
            Id = 1,
            Title = "Test RPG",
            Description = "A great RPG game",
            Genres = new List<Genre> { genre }
        };

        var session = new GameSession
        {
            GameId = 2,
            Game = new Game
            {
                Id = 2,
                Title = "Played Game",
                Genres = new List<Genre> { genre }
            },
            Duration = TimeSpan.FromHours(5)
        };

        _sessionRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession> { session });

        _gameRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Game> { game });

        // Act
        var result = await _engine.GetRecommendationsAsync(
            new RecommendationContext(userId, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.First().Title.Should().Be("Test RPG");
    }

    [Fact]
    public async Task GetPlayNextAsync_WithAvailableTime_ReturnsFittingGames()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var game = new Game
        {
            Id = 1,
            Title = "Quick Game",
            EstimatedTimeToComplete = TimeSpan.FromHours(10),
            Genres = new List<Genre> { new() { Name = "Arcade" } }
        };

        _sessionRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession>());

        _gameRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Game> { game });

        // Act
        var result = await _engine.GetPlayNextAsync(
            new PlayNextContext(userId, TimeSpan.FromHours(2), GamingMood.QuickSession));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProvideFeedbackAsync_StoresFeedback()
    {
        // Arrange
        var recommendationId = Guid.NewGuid();
        var feedback = new RecommendationFeedbackV2(
            RecommendationFeedbackType.Liked,
            "Great recommendation!",
            5);

        // Act
        var result = await _engine.ProvideFeedbackAsync(recommendationId, feedback);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshModelAsync_ReturnsSuccess()
    {
        // Act
        var result = await _engine.RefreshModelAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSocialRecommendationsAsync_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _engine.GetSocialRecommendationsAsync(userId, 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecommendationsAsync_WithFilters_AppliesFilters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new RecommendationFilters(
            Genres: new List<string> { "Action" },
            MinRating: 4.0f);

        var actionGame = new Game
        {
            Id = 1,
            Title = "Action Game",
            Rating = 4.5f,
            Genres = new List<Genre> { new() { Name = "Action" } }
        };

        var rpgGame = new Game
        {
            Id = 2,
            Title = "RPG Game",
            Rating = 3.5f,
            Genres = new List<Genre> { new() { Name = "RPG" } }
        };

        _sessionRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession>());

        _gameRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Game> { actionGame, rpgGame });

        // Act
        var result = await _engine.GetRecommendationsAsync(
            new RecommendationContext(userId, 10, filters));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(g => g.Title == "Action Game");
        result.Value.Should().NotContain(g => g.Title == "RPG Game");
    }

    [Fact]
    public void ContextualFactors_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var factors = new ContextualFactors(
            TimeOfDay: TimeOfDay.Evening,
            DayOfWeek: DayOfWeek.Saturday,
            AvailableTime: TimeSpan.FromHours(2),
            Mood: GamingMood.Immersive,
            IsWeekend: true,
            Location: "Home",
            DeviceType: "PC");

        // Assert
        factors.TimeOfDay.Should().Be(TimeOfDay.Evening);
        factors.DayOfWeek.Should().Be(DayOfWeek.Saturday);
        factors.AvailableTime.Should().Be(TimeSpan.FromHours(2));
        factors.Mood.Should().Be(GamingMood.Immersive);
        factors.IsWeekend.Should().BeTrue();
        factors.Location.Should().Be("Home");
        factors.DeviceType.Should().Be("PC");
    }

    [Fact]
    public void GameRecommendationV2_Properties_WorkCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var factors = new List<RecommendationFactor>
        {
            new("Genre Match", "Matches your preferences", 0.4f, 0.8f)
        };

        // Act
        var recommendation = new GameRecommendationV2(
            Id: id,
            GameId: gameId,
            Title: "Test Game",
            Description: "A test game",
            Reason: "Because it's great",
            ConfidenceScore: 0.85f,
            CollaborativeScore: 0.7f,
            ContentScore: 0.8f,
            ContextualScore: 0.6f,
            CoverArtUrl: "http://example.com/cover.jpg",
            MatchingTags: new List<string> { "RPG", "Fantasy" },
            Factors: factors,
            Source: RecommendationSourceV2.Hybrid,
            IsInLibrary: false,
            GeneratedAt: DateTime.UtcNow);

        // Assert
        recommendation.Id.Should().Be(id);
        recommendation.GameId.Should().Be(gameId);
        recommendation.Title.Should().Be("Test Game");
        recommendation.ConfidenceScore.Should().Be(0.85f);
        recommendation.Factors.Should().HaveCount(1);
        recommendation.Source.Should().Be(RecommendationSourceV2.Hybrid);
    }
}
