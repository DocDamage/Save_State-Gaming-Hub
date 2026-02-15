using System.Reflection;
using FluentAssertions;
using Moq;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Intelligence.Recommendations.Services;
using SaveState.Infrastructure.Intelligence.Recommendations;

using GenreEntity = SaveState.Core.GameLibrary.Entities.Genre;

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
        var game = CreateGame("Test RPG", ["RPG"], description: "A great RPG game");
        var playedGame = CreateGame("Played Game", ["RPG"]);
        var session = CreateSession(playedGame, TimeSpan.FromHours(5));

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
        var game = CreateGame(
            "Quick Game",
            ["Arcade"],
            estimatedTimeToComplete: TimeSpan.FromHours(10));

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
            Genres: new List<string> { "Action" });

        var actionGame = CreateGame("Action Game", ["Action"]);
        var rpgGame = CreateGame("RPG Game", ["RPG"]);
        var playedGame = CreateGame("Action History", ["Action"]);
        var session = CreateSession(playedGame, TimeSpan.FromHours(3));

        _sessionRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession> { session });

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

    private static Game CreateGame(
        string title,
        IEnumerable<string> genres,
        string? description = null,
        TimeSpan? estimatedTimeToComplete = null)
    {
        var game = Game.Create(title, description: description);

        foreach (var genre in genres)
        {
            game.Genres.Add(new GenreEntity(genre));
        }

        if (estimatedTimeToComplete.HasValue)
        {
            game.SetEstimatedTimeToComplete(estimatedTimeToComplete.Value);
        }

        return game;
    }

    private static GameSession CreateSession(Game game, TimeSpan duration, DateTime? startedAt = null)
    {
        var startTime = startedAt ?? DateTime.UtcNow - duration;
        var session = GameSession.Create(game.Id);

        SetPrivateProperty(session, nameof(GameSession.Game), game);
        SetPrivateProperty(session, nameof(GameSession.StartedAt), startTime);
        SetPrivateProperty(session, nameof(GameSession.EndedAt), startTime + duration);

        return session;
    }

    private static void SetPrivateProperty<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (property is null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' was not found on type '{target.GetType().Name}'.");
        }

        property.SetValue(target, value);
    }
}
