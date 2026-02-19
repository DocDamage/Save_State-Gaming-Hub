// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.Subscriptions;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Subscriptions;
using Xunit;

namespace SaveState.Application.Tests.Subscriptions;

/// <summary>
/// Unit tests for SubscriptionManagerService.
/// </summary>
public class SubscriptionManagerServiceTests
{
    private readonly Mock<ILogger<SubscriptionManagerService>> _loggerMock;
    private readonly Mock<ISubscriptionRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly List<Mock<ISubscriptionProvider>> _providerMocks;
    private readonly SubscriptionManagerService _service;
    private readonly DateTime _fixedTime = new(2026, 2, 17, 12, 0, 0, DateTimeKind.Utc);

    public SubscriptionManagerServiceTests()
    {
        _loggerMock = new Mock<ILogger<SubscriptionManagerService>>();
        _repositoryMock = new Mock<ISubscriptionRepository>();
        _cacheMock = new Mock<ICacheService>();
        _timeProviderMock = new Mock<ITimeProvider>();
        _providerMocks = new List<Mock<ISubscriptionProvider>>
        {
            CreateProviderMock(SubscriptionServiceType.XboxGamePass),
            CreateProviderMock(SubscriptionServiceType.PlayStationPlus)
        };

        _timeProviderMock.Setup(t => t.UtcNow).Returns(_fixedTime);

        _service = new SubscriptionManagerService(
            _loggerMock.Object,
            _providerMocks.Select(m => m.Object),
            _repositoryMock.Object,
            _cacheMock.Object,
            _timeProviderMock.Object);
    }

    private Mock<ISubscriptionProvider> CreateProviderMock(SubscriptionServiceType serviceType)
    {
        var mock = new Mock<ISubscriptionProvider>();
        mock.Setup(p => p.ServiceType).Returns(serviceType);
        return mock;
    }

    #region GetAvailableServicesAsync Tests

    [Fact]
    public async Task GetAvailableServicesAsync_ShouldReturnServicesFromAllProviders()
    {
        // Arrange
        var xboxInfo = new SubscriptionServiceInfo
        {
            Name = "Xbox Game Pass",
            MonthlyPrice = 9.99m,
            GameCount = 400
        };

        var psInfo = new SubscriptionServiceInfo
        {
            Name = "PlayStation Plus",
            MonthlyPrice = 9.99m,
            GameCount = 700
        };

        _providerMocks[0].Setup(p => p.GetServiceInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(xboxInfo));
        _providerMocks[1].Setup(p => p.GetServiceInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(psInfo));

        // Act
        var result = await _service.GetAvailableServicesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(s => s.Name == "Xbox Game Pass");
        result.Value.Should().Contain(s => s.Name == "PlayStation Plus");
    }

    [Fact]
    public async Task GetAvailableServicesAsync_ShouldReturnFailure_WhenExceptionOccurs()
    {
        // Arrange
        _providerMocks[0].Setup(p => p.GetServiceInfoAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act
        var result = await _service.GetAvailableServicesAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region CompareSubscriptionsAsync Tests

    [Fact]
    public async Task CompareSubscriptionsAsync_ShouldCalculateCorrectTotals()
    {
        // Arrange
        var services = new List<SubscriptionServiceInfo>
        {
            new() { Name = "Service A", MonthlyPrice = 10m, GameCount = 100 },
            new() { Name = "Service B", MonthlyPrice = 15m, GameCount = 200 }
        };

        _providerMocks[0].Setup(p => p.GetServiceInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(services[0]));
        _providerMocks[1].Setup(p => p.GetServiceInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(services[1]));

        // Act
        var result = await _service.CompareSubscriptionsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalMonthlyCost.Should().Be(25m);
        result.Value.TotalUniqueGames.Should().Be(300);
    }

    [Fact]
    public async Task CompareSubscriptionsAsync_ShouldIdentifyBestValue()
    {
        // Arrange
        var services = new List<SubscriptionServiceInfo>
        {
            new() { Name = "Expensive", MonthlyPrice = 20m, GameCount = 100 },
            new() { Name = "Best Value", MonthlyPrice = 10m, GameCount = 200 } // 20 games/$
        };

        _providerMocks[0].Setup(p => p.GetServiceInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(services[0]));
        _providerMocks[1].Setup(p => p.GetServiceInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(services[1]));

        // Act
        var result = await _service.CompareSubscriptionsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BestValueRecommendation.Should().Contain("Best Value");
    }

    #endregion

    #region GetLeavingSoonGamesAsync Tests

    [Fact]
    public async Task GetLeavingSoonGamesAsync_ShouldAggregateFromAllProviders()
    {
        // Arrange
        var xboxGames = new List<SubscriptionGame>
        {
            new() { Title = "Game 1", LeavingSoonDate = _fixedTime.AddDays(5) },
            new() { Title = "Game 2", LeavingSoonDate = _fixedTime.AddDays(10) }
        };

        var psGames = new List<SubscriptionGame>
        {
            new() { Title = "Game 3", LeavingSoonDate = _fixedTime.AddDays(7) }
        };

        _providerMocks[0].Setup(p => p.GetLeavingSoonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SubscriptionGame>>(xboxGames));
        _providerMocks[1].Setup(p => p.GetLeavingSoonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SubscriptionGame>>(psGames));

        // Act
        var result = await _service.GetLeavingSoonGamesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetLeavingSoonGamesAsync_ShouldSortByUrgency()
    {
        // Arrange
        var games = new List<SubscriptionGame>
        {
            new() { Title = "Later", LeavingSoonDate = _fixedTime.AddDays(10) },
            new() { Title = "Soon", LeavingSoonDate = _fixedTime.AddDays(3) },
            new() { Title = "Very Soon", LeavingSoonDate = _fixedTime.AddDays(1) }
        };

        _providerMocks[0].Setup(p => p.GetLeavingSoonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SubscriptionGame>>(games));
        _providerMocks[1].Setup(p => p.GetLeavingSoonAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SubscriptionGame>>(new List<SubscriptionGame>()));

        // Act
        var result = await _service.GetLeavingSoonGamesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.First().Game.Title.Should().Be("Very Soon");
        result.Value.Last().Game.Title.Should().Be("Later");
    }

    #endregion

    #region SearchGamesAsync Tests

    [Fact]
    public async Task SearchGamesAsync_ShouldReturnMatchingGames()
    {
        // Arrange
        var library = new UserSubscriptionLibrary
        {
            Games = new List<SubscriptionGame>
            {
                new() { Title = "The Legend of Zelda", Genres = new List<string> { "Adventure" } },
                new() { Title = "Super Mario Bros", Genres = new List<string> { "Platformer" } },
                new() { Title = "Zelda II", Genres = new List<string> { "Adventure" } }
            }
        };

        // Mock the library call through providers
        _providerMocks[0].Setup(p => p.GetGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SubscriptionGame>>(library.Games));
        _providerMocks[0].Setup(p => p.IsSubscribedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _providerMocks[1].Setup(p => p.IsSubscribedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _service.SearchGamesAsync("Zelda");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(g => g.Title == "The Legend of Zelda");
        result.Value.Should().Contain(g => g.Title == "Zelda II");
    }

    [Fact]
    public async Task SearchGamesAsync_ShouldSearchGenres()
    {
        // Arrange
        var library = new UserSubscriptionLibrary
        {
            Games = new List<SubscriptionGame>
            {
                new() { Title = "Game 1", Genres = new List<string> { "RPG", "Adventure" } },
                new() { Title = "Game 2", Genres = new List<string> { "Shooter" } }
            }
        };

        _providerMocks[0].Setup(p => p.GetGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SubscriptionGame>>(library.Games));
        _providerMocks[0].Setup(p => p.IsSubscribedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _providerMocks[1].Setup(p => p.IsSubscribedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _service.SearchGamesAsync("RPG");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Title.Should().Be("Game 1");
    }

    #endregion

    #region TrackGameAsync Tests

    [Fact]
    public async Task TrackGameAsync_ShouldAddTrackedGame()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameTitle = "Test Game";

        _repositoryMock.Setup(r => r.AddTrackedGameAsync(It.IsAny<TrackedGameEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.TrackGameAsync(userId, gameTitle);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _repositoryMock.Verify(r => r.AddTrackedGameAsync(
            It.Is<TrackedGameEntity>(t => t.UserId == userId && t.GameTitle == gameTitle),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SyncLibraryAsync Tests

    [Fact]
    public async Task SyncLibraryAsync_ShouldSaveGamesAndClearCache()
    {
        // Arrange
        var games = new List<SubscriptionGame>
        {
            new() { Title = "Game 1" },
            new() { Title = "Game 2" }
        };

        _providerMocks[0].Setup(p => p.GetGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SubscriptionGame>>(games));
        _providerMocks[1].Setup(p => p.GetGamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SubscriptionGame>>(new List<SubscriptionGame>()));

        _repositoryMock.Setup(r => r.SaveGamesAsync(It.IsAny<IEnumerable<SubscriptionGame>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SyncLibraryAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(r => r.SaveGamesAsync(
            It.Is<IEnumerable<SubscriptionGame>>(g => g.Count() == 2),
            It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.Remove("subscription_library"), Times.Once);
    }

    #endregion
}
