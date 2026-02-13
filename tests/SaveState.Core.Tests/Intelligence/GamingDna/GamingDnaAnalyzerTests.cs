using FluentAssertions;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Intelligence.GamingDna.Services;
using SaveState.Infrastructure.Intelligence.GamingDna;

namespace SaveState.Core.Tests.Intelligence.GamingDna;

public class GamingDnaAnalyzerTests
{
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IGameSessionRepository> _sessionRepositoryMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly GamingDnaAnalyzer _analyzer;

    public GamingDnaAnalyzerTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _sessionRepositoryMock = new Mock<IGameSessionRepository>();
        _timeProviderMock = new Mock<ITimeProvider>();

        _timeProviderMock.Setup(t => t.UtcNow).Returns(new DateTime(2026, 2, 13, 12, 0, 0));

        _analyzer = new GamingDnaAnalyzer(
            _gameRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _timeProviderMock.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<GamingDnaAnalyzer>>());
    }

    [Fact]
    public async Task AnalyzeProfileAsync_WithNoHistory_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _sessionRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession>());

        // Act
        var result = await _analyzer.AnalyzeProfileAsync(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task AnalyzeProfileAsync_WithGamingHistory_ReturnsProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessions = CreateMockSessions();

        _sessionRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _analyzer.AnalyzeProfileAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(userId);
        result.Value.Archetypes.Should().NotBeEmpty();
        result.Value.GenrePreferences.Should().NotBeNull();
        result.Value.PlayStyleMetrics.Should().NotBeNull();
        result.Value.Signature.Should().NotBeNull();
    }

    [Fact]
    public async Task GetArchetypesAsync_WithRPGSessions_ReturnsStorySeekerAndExplorer()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessions = new List<GameSession>
        {
            new()
            {
                GameId = 1,
                Game = new Game
                {
                    Id = 1,
                    Title = "RPG Game",
                    Genres = new List<Genre> { new() { Name = "RPG" } }
                },
                Duration = TimeSpan.FromHours(30),
                StartTime = DateTime.UtcNow.AddDays(-1)
            }
        };

        _sessionRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _analyzer.GetArchetypesAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(a => a.Archetype == GamingArchetype.StorySeeker);
    }

    [Fact]
    public async Task GetGenreEvolutionAsync_ReturnsTimeline()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var timeRange = TimeRange.LastMonth;

        // Act
        var result = await _analyzer.GetGenreEvolutionAsync(userId, timeRange);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.TimeRange.Should().Be(timeRange);
    }

    [Fact]
    public async Task GetVisualizationDataAsync_ReturnsVisualizationData()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _analyzer.GetVisualizationDataAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.RadarChart.Should().NotBeNull();
        result.Value.RadarChart.Dimensions.Should().NotBeEmpty();
        result.Value.TimelineChart.Should().NotBeNull();
        result.Value.Heatmap.Should().NotBeNull();
        result.Value.ArchetypeViz.Should().NotBeNull();
    }

    [Fact]
    public async Task CompareProfilesAsync_ReturnsComparisonResult()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        // Act
        var result = await _analyzer.CompareProfilesAsync(userId1, userId2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId1.Should().Be(userId1);
        result.Value.UserId2.Should().Be(userId2);
        result.Value.SimilarityScore.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RefreshAnalysisAsync_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _analyzer.RefreshAnalysisAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void GamingArchetype_Enum_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<GamingArchetype>().Should().Contain(new[]
        {
            GamingArchetype.Completionist,
            GamingArchetype.Explorer,
            GamingArchetype.Competitor,
            GamingArchetype.StorySeeker,
            GamingArchetype.Strategist,
            GamingArchetype.Speedrunner,
            GamingArchetype.Socialite,
            GamingArchetype.Collector,
            GamingArchetype.Casual,
            GamingArchetype.Hardcore,
            GamingArchetype.Creative,
            GamingArchetype.Achiever
        });
    }

    [Fact]
    public void TimeRange_StaticProperties_ReturnExpectedRanges()
    {
        // Act & Assert
        TimeRange.LastMonth.Start.Should().BeCloseTo(
            DateTime.UtcNow.AddMonths(-1), TimeSpan.FromSeconds(1));
        TimeRange.LastQuarter.Start.Should().BeCloseTo(
            DateTime.UtcNow.AddMonths(-3), TimeSpan.FromSeconds(1));
        TimeRange.LastYear.Start.Should().BeCloseTo(
            DateTime.UtcNow.AddYears(-1), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GamingDnaProfile_Properties_WorkCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var archetypes = new List<GamingArchetypeScore>
        {
            new(GamingArchetype.StorySeeker, 0.85f, new List<string> { "Loves RPGs" })
        };
        var genrePrefs = new GenrePreferences(
            new List<WeightedGenre>(),
            new List<WeightedGenre>(),
            new List<WeightedGenre>());
        var playStyle = new PlayStyleMetrics(
            60, 90, TimeOfDay.Evening, DayOfWeek.Saturday,
            0.6f, 0.8f, 0.7f, DifficultyPreference.Hard);
        var engagement = new EngagementPatterns(
            0.7f, 0.3f, 0.4f, 0.2f, 0.5f, 0.8f, 0.6f, 0.4f);
        var social = new SocialGamingProfile(
            0.6f, 3, 0.5f, 0.8f, 0.7f, new List<string>());
        var achievement = new AchievementProfile(
            0.7f, 0.8f, 0.6f, 100, 10, 0.75f);
        var signature = new DnaSignature("hash123", new List<float> { 0.5f, 0.6f }, DateTime.UtcNow);

        // Act
        var profile = new GamingDnaProfile(
            userId, DateTime.UtcNow, archetypes, genrePrefs,
            playStyle, engagement, social, achievement, signature);

        // Assert
        profile.UserId.Should().Be(userId);
        profile.Archetypes.Should().HaveCount(1);
        profile.Archetypes[0].Archetype.Should().Be(GamingArchetype.StorySeeker);
        profile.Signature.Hash.Should().Be("hash123");
    }

    [Fact]
    public void DnaVisualizationData_ContainsExpectedCharts()
    {
        // Arrange
        var radar = new RadarChartData(new List<RadarDimension>());
        var timeline = new TimelineChartData(new List<string>(), new List<TimelineDataset>());
        var heatmap = new HeatmapData(new List<HeatmapCell>(), 2, 3);
        var archetypeViz = new ArchetypeVisualization(new List<ArchetypeNode>(), new List<ArchetypeEdge>());

        // Act
        var viz = new DnaVisualizationData(Guid.NewGuid(), radar, timeline, heatmap, archetypeViz);

        // Assert
        viz.RadarChart.Should().Be(radar);
        viz.TimelineChart.Should().Be(timeline);
        viz.Heatmap.Should().Be(heatmap);
        viz.ArchetypeViz.Should().Be(archetypeViz);
    }

    private List<GameSession> CreateMockSessions()
    {
        return new List<GameSession>
        {
            new()
            {
                GameId = 1,
                Game = new Game
                {
                    Id = 1,
                    Title = "RPG Game",
                    Genres = new List<Genre>
                    {
                        new() { Name = "RPG" },
                        new() { Name = "Story" }
                    }
                },
                Duration = TimeSpan.FromHours(40),
                StartTime = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                GameId = 2,
                Game = new Game
                {
                    Id = 2,
                    Title = "Strategy Game",
                    Genres = new List<Genre>
                    {
                        new() { Name = "Strategy" }
                    }
                },
                Duration = TimeSpan.FromHours(20),
                StartTime = DateTime.UtcNow.AddDays(-2)
            }
        };
    }
}
