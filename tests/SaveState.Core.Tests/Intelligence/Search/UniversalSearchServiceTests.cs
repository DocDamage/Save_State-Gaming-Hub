using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Intelligence.Search.Services;
using SaveState.Infrastructure.Intelligence.Search;

namespace SaveState.Core.Tests.Intelligence.Search;

public class UniversalSearchServiceTests
{
    private readonly UniversalSearchService _service;
    private readonly Mock<ILogger<UniversalSearchService>> _loggerMock;

    public UniversalSearchServiceTests()
    {
        _loggerMock = new Mock<ILogger<UniversalSearchService>>();
        _service = new UniversalSearchService(_loggerMock.Object);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyIndex_ReturnsEmptyResults()
    {
        // Arrange
        var query = new SearchQuery("RPG", 20);

        // Act
        var result = await _service.SearchAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalResults.Should().Be(0);
        result.Value.Games.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithIndexedGames_ReturnsMatchingGames()
    {
        // Arrange
        var game = new GameSearchIndex(
            Id: Guid.NewGuid(),
            Title: "Epic RPG Adventure",
            Description: "An amazing RPG",
            Genres: new List<string> { "RPG", "Fantasy" },
            Tags: new List<string> { "Open World", "Magic" },
            Features: new List<string>(),
            Developer: "Game Studio",
            Publisher: "Publisher",
            ReleaseDate: DateTime.UtcNow.AddYears(-1),
            Rating: 4.5f,
            IndexedAt: DateTime.UtcNow);

        await _service.IndexGameAsync(game);

        var query = new SearchQuery("RPG", 20);

        // Act
        var result = await _service.SearchAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Games.Should().HaveCount(1);
        result.Value.Games.First().Title.Should().Be("Epic RPG Adventure");
    }

    [Fact]
    public async Task SearchGamesAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var rpgGame = new GameSearchIndex(
            Guid.NewGuid(), "RPG Game", "An RPG",
            new List<string> { "RPG" }, new List<string>(), new List<string>(),
            null, null, null, 4.0f, DateTime.UtcNow);

        var actionGame = new GameSearchIndex(
            Guid.NewGuid(), "Action Game", "Action",
            new List<string> { "Action" }, new List<string>(), new List<string>(),
            null, null, null, 4.5f, DateTime.UtcNow);

        await _service.IndexGameAsync(rpgGame);
        await _service.IndexGameAsync(actionGame);

        // Act
        var result = await _service.SearchGamesAsync("Game",
            new GameSearchOptions(Genres: new List<string> { "RPG" }));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(g => g.Title == "RPG Game");
    }

    [Fact]
    public async Task SearchActionsAsync_ReturnsMatchingActions()
    {
        // Arrange
        var action = new SearchableAction(
            Id: "settings-theme",
            Title: "Change Theme",
            Description: "Change application theme",
            ActionType: "Settings",
            Category: "Appearance",
            Icon: "🎨",
            Keywords: new List<string> { "theme", "color", "appearance" },
            Tags: null);

        await _service.RegisterActionAsync(action);

        // Act
        var result = await _service.SearchActionsAsync("theme");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(a => a.Title == "Change Theme");
    }

    [Fact]
    public async Task IndexContentAsync_AndSearchContent_ReturnsContent()
    {
        // Arrange
        var content = new ContentIndex(
            Id: Guid.NewGuid(),
            Type: ContentType.Review,
            Title: "Great RPG Review",
            Content: "This RPG is amazing with great story",
            SourceId: "game-123",
            SourceType: "Game",
            CreatedAt: DateTime.UtcNow,
            AuthorId: "user-456",
            Tags: new List<string> { "RPG", "Review" },
            IndexedAt: DateTime.UtcNow);

        await _service.IndexContentAsync(content);

        // Act
        var result = await _service.SearchContentAsync("amazing RPG");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WithPartialQuery_ReturnsSuggestions()
    {
        // Arrange
        var game = new GameSearchIndex(
            Guid.NewGuid(), "Final Fantasy", "RPG",
            new List<string> { "RPG" }, new List<string>(), new List<string>(),
            null, null, null, null, DateTime.UtcNow);

        await _service.IndexGameAsync(game);

        // Act
        var result = await _service.GetSuggestionsAsync("Fan", 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(s => s.Text == "Final Fantasy");
    }

    [Fact]
    public async Task GetTrendingSearchesAsync_AfterMultipleSearches_ReturnsTrends()
    {
        // Arrange
        await _service.SearchAsync(new SearchQuery("RPG", 20));
        await _service.SearchAsync(new SearchQuery("RPG", 20));
        await _service.SearchAsync(new SearchQuery("Action", 20));

        // Act
        var result = await _service.GetTrendingSearchesAsync(10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(t => t.Query == "rpg" && t.SearchCount == 2);
    }

    [Fact]
    public void SearchCategory_Enum_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<SearchCategory>().Should().Contain(new[]
        {
            SearchCategory.Games,
            SearchCategory.SaveStates,
            SearchCategory.Actions,
            SearchCategory.Settings,
            SearchCategory.Reviews,
            SearchCategory.Guides,
            SearchCategory.Community,
            SearchCategory.All
        });
    }

    [Fact]
    public void ContentType_Enum_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<ContentType>().Should().Contain(new[]
        {
            ContentType.Review,
            ContentType.Guide,
            ContentType.News,
            ContentType.ForumPost,
            ContentType.WikiArticle,
            ContentType.Description,
            ContentType.Note,
            ContentType.AchievementDescription
        });
    }

    [Fact]
    public void GameMatchReason_Enum_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<GameMatchReason>().Should().Contain(new[]
        {
            GameMatchReason.TitleMatch,
            GameMatchReason.GenreMatch,
            GameMatchReason.TagMatch,
            GameMatchReason.DescriptionMatch,
            GameMatchReason.SemanticMatch,
            GameMatchReason.SimilarGames,
            GameMatchReason.Trending,
            GameMatchReason.RecentlyPlayed
        });
    }

    [Fact]
    public void SearchQuery_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var query = new SearchQuery(
            "RPG games",
            10,
            new List<SearchCategory> { SearchCategory.Games },
            new SearchFilters(null, null, null, null, 0.5f));

        // Assert
        query.Query.Should().Be("RPG games");
        query.MaxResults.Should().Be(10);
        query.Categories.Should().ContainSingle();
        query.Filters!.MinRelevanceScore.Should().Be(0.5f);
    }

    [Fact]
    public void UniversalSearchResults_Properties_WorkCorrectly()
    {
        // Arrange
        var games = new List<GameSearchResult>();
        var saveStates = new List<SaveStateSearchResult>();
        var actions = new List<ActionSearchResult>();
        var content = new List<ContentSearchResult>();

        // Act
        var results = new UniversalSearchResults(
            "query", games, saveStates, actions, content, 0, TimeSpan.FromMilliseconds(100));

        // Assert
        results.Query.Should().Be("query");
        results.TotalResults.Should().Be(0);
        results.SearchDuration.Should().BeCloseTo(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void SemanticGameResult_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var result = new SemanticGameResult(
            Guid.NewGuid(),
            "Test Game",
            "Description",
            0.9f,
            0.8f,
            0.85f,
            new List<string> { "RPG", "Fantasy" },
            "Matched on genre");

        // Assert
        result.Title.Should().Be("Test Game");
        result.SemanticScore.Should().Be(0.9f);
        result.CombinedScore.Should().Be(0.85f);
        result.MatchedConcepts.Should().Contain("RPG");
    }

    [Fact]
    public void ContentSnippet_Properties_WorkCorrectly()
    {
        // Arrange & Act
        var snippet = new ContentSnippet("Matched text here", 10, 20, true);

        // Assert
        snippet.Text.Should().Be("Matched text here");
        snippet.StartIndex.Should().Be(10);
        snippet.Length.Should().Be(20);
        snippet.IsMatch.Should().BeTrue();
    }
}
