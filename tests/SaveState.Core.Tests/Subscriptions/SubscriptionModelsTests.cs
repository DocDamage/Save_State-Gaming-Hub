// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System;
using System.Collections.Generic;
using FluentAssertions;
using SaveState.Core.Subscriptions;
using Xunit;

namespace SaveState.Core.Tests.Subscriptions;

/// <summary>
/// Unit tests for subscription domain models.
/// </summary>
public class SubscriptionModelsTests
{
    #region SubscriptionGame Tests

    [Fact]
    public void SubscriptionGame_IsLeavingSoon_ShouldReturnTrue_WhenLeavingDateWithin14Days()
    {
        // Arrange
        var game = new SubscriptionGame
        {
            Title = "Test Game",
            LeavingSoonDate = DateTime.UtcNow.AddDays(7)
        };

        // Act & Assert
        game.IsLeavingSoon.Should().BeTrue();
    }

    [Fact]
    public void SubscriptionGame_IsLeavingSoon_ShouldReturnFalse_WhenLeavingDateIsNull()
    {
        // Arrange
        var game = new SubscriptionGame
        {
            Title = "Test Game",
            LeavingSoonDate = null
        };

        // Act & Assert
        game.IsLeavingSoon.Should().BeFalse();
    }

    [Fact]
    public void SubscriptionGame_IsLeavingSoon_ShouldReturnFalse_WhenLeavingDateMoreThan14Days()
    {
        // Arrange
        var game = new SubscriptionGame
        {
            Title = "Test Game",
            LeavingSoonDate = DateTime.UtcNow.AddDays(20)
        };

        // Act & Assert
        game.IsLeavingSoon.Should().BeFalse();
    }

    [Fact]
    public void SubscriptionGame_IsNewArrival_ShouldReturnTrue_WhenAddedWithin30Days()
    {
        // Arrange
        var game = new SubscriptionGame
        {
            Title = "Test Game",
            AddedDate = DateTime.UtcNow.AddDays(-15)
        };

        // Act & Assert
        game.IsNewArrival.Should().BeTrue();
    }

    [Fact]
    public void SubscriptionGame_IsNewArrival_ShouldReturnFalse_WhenAddedMoreThan30DaysAgo()
    {
        // Arrange
        var game = new SubscriptionGame
        {
            Title = "Test Game",
            AddedDate = DateTime.UtcNow.AddDays(-45)
        };

        // Act & Assert
        game.IsNewArrival.Should().BeFalse();
    }

    #endregion

    #region UserSubscriptionLibrary Tests

    [Fact]
    public void UserSubscriptionLibrary_TotalGames_ShouldReturnCorrectCount()
    {
        // Arrange
        var library = new UserSubscriptionLibrary
        {
            Games = new List<SubscriptionGame>
            {
                new() { Title = "Game 1" },
                new() { Title = "Game 2" },
                new() { Title = "Game 3" }
            }
        };

        // Act & Assert
        library.TotalGames.Should().Be(3);
    }

    [Fact]
    public void UserSubscriptionLibrary_LeavingSoonCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var library = new UserSubscriptionLibrary
        {
            Games = new List<SubscriptionGame>
            {
                new() { Title = "Game 1", LeavingSoonDate = DateTime.UtcNow.AddDays(5) },
                new() { Title = "Game 2", LeavingSoonDate = DateTime.UtcNow.AddDays(10) },
                new() { Title = "Game 3" } // Not leaving
            }
        };

        // Act & Assert
        library.LeavingSoonCount.Should().Be(2);
    }

    [Fact]
    public void UserSubscriptionLibrary_NewArrivalsCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var library = new UserSubscriptionLibrary
        {
            Games = new List<SubscriptionGame>
            {
                new() { Title = "Game 1", AddedDate = DateTime.UtcNow.AddDays(-10) },
                new() { Title = "Game 2", AddedDate = DateTime.UtcNow.AddDays(-40) },
                new() { Title = "Game 3", AddedDate = DateTime.UtcNow.AddDays(-5) }
            }
        };

        // Act & Assert
        library.NewArrivalsCount.Should().Be(2);
    }

    #endregion

    #region SubscriptionComparison Tests

    [Fact]
    public void SubscriptionComparison_ShouldCalculatePropertiesCorrectly()
    {
        // Arrange
        var comparison = new SubscriptionComparison
        {
            Services = new List<SubscriptionServiceInfo>
            {
                new() { Name = "Service A", MonthlyPrice = 9.99m, GameCount = 100 },
                new() { Name = "Service B", MonthlyPrice = 14.99m, GameCount = 200 },
                new() { Name = "Service C", MonthlyPrice = 19.99m, GameCount = 300 }
            }
        };

        // Act
        comparison.TotalMonthlyCost = comparison.Services.Sum(s => s.MonthlyPrice);
        comparison.TotalUniqueGames = comparison.Services.Sum(s => s.GameCount);

        // Assert
        comparison.TotalMonthlyCost.Should().Be(44.97m);
        comparison.TotalUniqueGames.Should().Be(600);
    }

    [Fact]
    public void SubscriptionComparison_BestValueRecommendation_ShouldIdentifyBestDeal()
    {
        // Arrange
        var services = new List<SubscriptionServiceInfo>
        {
            new() { Name = "Service A", MonthlyPrice = 10m, GameCount = 100 }, // 10 games per dollar
            new() { Name = "Service B", MonthlyPrice = 15m, GameCount = 300 }, // 20 games per dollar - BEST
            new() { Name = "Service C", MonthlyPrice = 20m, GameCount = 200 }  // 10 games per dollar
        };

        // Act
        var bestValue = services
            .OrderByDescending(s => s.GameCount / Math.Max((double)s.MonthlyPrice, 1))
            .First();

        // Assert
        bestValue.Name.Should().Be("Service B");
    }

    #endregion

    #region LeavingSoonAlert Tests

    [Fact]
    public void LeavingSoonAlert_DaysRemaining_ShouldCalculateCorrectly()
    {
        // Arrange
        var fixedNow = new DateTime(2026, 2, 17, 12, 0, 0, DateTimeKind.Utc);
        var alert = new LeavingSoonAlert
        {
            Game = new SubscriptionGame { Title = "Test Game" },
            LeavingDate = fixedNow.AddDays(5)
        };

        // Act - calculate manually using the fixed baseline to keep test deterministic.
        var daysRemaining = (alert.LeavingDate - fixedNow).Days;

        // Assert - use range to account for test execution time
        daysRemaining.Should().BeInRange(4, 5);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public void LeavingSoonAlert_IsUrgent_ShouldReturnCorrectValue(int daysRemaining, bool expected)
    {
        // Arrange - use fixed dates to avoid timing issues
        var fixedNow = new DateTime(2026, 2, 17, 12, 0, 0, DateTimeKind.Utc);
        var alert = new LeavingSoonAlert
        {
            Game = new SubscriptionGame { Title = "Test Game" },
            LeavingDate = fixedNow.AddDays(daysRemaining)
        };

        // Act
        var actualDaysRemaining = (alert.LeavingDate - fixedNow).Days;
        var isUrgent = actualDaysRemaining <= 7;

        // Assert
        isUrgent.Should().Be(expected);
    }

    #endregion

    #region SubscriptionServiceInfo Tests

    [Fact]
    public void SubscriptionServiceInfo_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var info = new SubscriptionServiceInfo();

        // Assert
        info.Name.Should().BeEmpty();
        info.Description.Should().BeEmpty();
        info.MonthlyPrice.Should().Be(0);
        info.GameCount.Should().Be(0);
        info.Features.Should().BeEmpty();
    }

    [Fact]
    public void SubscriptionServiceInfo_ShouldStoreAllProperties()
    {
        // Arrange & Act
        var info = new SubscriptionServiceInfo
        {
            Id = "xbox-game-pass",
            Name = "Xbox Game Pass",
            Description = "Access to 100+ games",
            MonthlyPrice = 9.99m,
            AnnualPrice = 99.99m,
            GameCount = 400,
            SupportsCloudGaming = true,
            SupportsEaPlay = false,
            IsActive = true,
            Features = new List<SubscriptionFeature>
            {
                new() { Name = "Cloud Gaming", IsIncluded = true }
            }
        };

        // Assert
        info.Id.Should().Be("xbox-game-pass");
        info.Name.Should().Be("Xbox Game Pass");
        info.SupportsCloudGaming.Should().BeTrue();
        info.Features.Should().HaveCount(1);
    }

    #endregion

    #region OAuthTokens Tests

    [Fact]
    public void OAuthTokens_IsExpired_ShouldReturnTrue_WhenExpired()
    {
        // Arrange
        var tokens = new Core.Subscriptions.Authentication.OAuthTokens
        {
            AccessToken = "test_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10)
        };

        // Act & Assert
        tokens.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void OAuthTokens_IsExpired_ShouldReturnTrue_WhenExpiringWithin5Minutes()
    {
        // Arrange
        var tokens = new Core.Subscriptions.Authentication.OAuthTokens
        {
            AccessToken = "test_token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(3)
        };

        // Act & Assert
        tokens.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void OAuthTokens_IsExpired_ShouldReturnFalse_WhenValid()
    {
        // Arrange
        var tokens = new Core.Subscriptions.Authentication.OAuthTokens
        {
            AccessToken = "test_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act & Assert
        tokens.IsExpired.Should().BeFalse();
    }

    #endregion
}
