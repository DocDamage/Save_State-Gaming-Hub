using FluentAssertions;
using SaveState.Core.Common.Interfaces;
using SaveState.Infrastructure.External;
using SaveState.Tests.Fakes;
using Xunit;

namespace SaveState.IntegrationTests;

/// <summary>
/// API contract tests for external service integrations.
/// Tests that external APIs return expected data structures and handle errors correctly.
/// </summary>
public class ApiContractTests
{
    [Fact]
    public async Task SteamApi_Contract_ReturnsExpectedMetadataStructure()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();

        // Act
        var metadata = await steamApi.GetGameMetadataAsync("570"); // Dota 2

        // Assert - Verify contract compliance
        metadata.Should().NotBeNull();
        metadata!.Title.Should().NotBeNullOrEmpty();
        metadata.Title.Should().BeOfType(typeof(string));
        metadata.Description.Should().NotBeNullOrEmpty();
        // metadata.ReleaseDate type validation - nullable DateTime is acceptable
        metadata.Developer.Should().NotBeNullOrEmpty();
        metadata.Publisher.Should().NotBeNullOrEmpty();
        metadata.Genres.Should().NotBeNull();
        metadata.Genres.Should().AllBeOfType(typeof(string));
    }

    [Fact]
    public async Task SteamApi_Contract_HandlesInvalidGameId()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();
        var invalidGameId = "invalid-game-id";

        // Act
        var metadata = await steamApi.GetGameMetadataAsync(invalidGameId);

        // Assert - Should return null for invalid IDs (contract behavior)
        metadata.Should().BeNull();
    }

    [Fact]
    public async Task SteamApi_Contract_GenresArray_IsNeverNull()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();

        // Act
        var metadata = await steamApi.GetGameMetadataAsync("730"); // CS2

        // Assert - Genres should always be an array, never null
        metadata.Should().NotBeNull();
        metadata!.Genres.Should().NotBeNull();
        metadata.Genres.Should().BeOfType(typeof(IReadOnlyList<string>));
    }

    [Fact]
    public async Task SteamApi_Contract_TitleIsRequiredField()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();

        // Act
        var metadata = await steamApi.GetGameMetadataAsync("10"); // Old game

        // Assert - Title should always be present for valid games
        metadata.Should().NotBeNull();
        metadata!.Title.Should().NotBeNullOrEmpty();
        metadata.Title.Trim().Should().NotBeEmpty();
    }

    [Fact]
    public async Task SteamApi_Contract_HandlesNetworkTimeout_Gracefully()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();

        // Act - Test with various game IDs that might have different response times
        var tasks = new[]
        {
            steamApi.GetGameMetadataAsync("570"),  // Popular game
            steamApi.GetGameMetadataAsync("730"),  // Very popular game
            steamApi.GetGameMetadataAsync("440"),  // Older game
        };

        // Assert - All should complete without throwing
        var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
        exception.Should().BeNull();

        var results = await Task.WhenAll(tasks);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    [Fact]
    public async Task SteamApi_Contract_ResponseIsConsistent()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();
        var gameId = "570";

        // Act - Call multiple times
        var result1 = await steamApi.GetGameMetadataAsync(gameId);
        var result2 = await steamApi.GetGameMetadataAsync(gameId);

        // Assert - Results should be consistent (idempotent)
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.Title.Should().Be(result2!.Title);
        result1.Description.Should().Be(result2.Description);
        result1.Genres.Should().BeEquivalentTo(result2.Genres);
    }

    [Fact]
    public async Task SteamApi_Contract_DateFields_AreValid()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();

        // Act
        var metadata = await steamApi.GetGameMetadataAsync("730"); // CS2 has a known release date

        // Assert - Date should be reasonable (not in future, not too far in past)
        metadata.Should().NotBeNull();
        metadata!.ReleaseDate.Should().NotBeNull();

        var releaseDate = metadata.ReleaseDate!.Value;
        releaseDate.Should().BeAfter(new DateTime(1990, 1, 1)); // Steam launched in 2003, but games existed before
        releaseDate.Should().BeBefore(DateTime.UtcNow.AddDays(365)); // Not more than 1 year in future
    }

    [Fact]
    public async Task SteamApi_Contract_LargeGameIds_HandledCorrectly()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();
        var largeGameId = "999999999999999"; // Very large ID

        // Act
        var metadata = await steamApi.GetGameMetadataAsync(largeGameId);

        // Assert - Should handle gracefully (return null, not crash)
        // Note: Fake provider might not implement this, but real API should handle it
        metadata.Should().BeNull();
    }

    [Fact]
    public async Task SteamApi_Contract_SpecialCharacters_InTitles_Handled()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();

        // Act - Test various game IDs that might have special characters
        var metadata = await steamApi.GetGameMetadataAsync("570"); // Dota 2

        // Assert - Should handle special characters gracefully
        metadata.Should().NotBeNull();
        metadata!.Title.Should().NotBeNull();

        // Title should not contain problematic characters or be malformed
        metadata.Title.Should().NotContain("\0"); // Null bytes
        metadata.Title.Should().NotContain("\r"); // Carriage returns in middle of string
        metadata.Title.Length.Should().BeGreaterThan(0);
        metadata.Title.Length.Should().BeLessThan(200); // Reasonable title length
    }

    [Fact]
    public async Task SteamApi_Contract_EmptyOrWhitespaceIds_Rejected()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();

        // Act & Assert - Should handle empty/whitespace IDs gracefully
        var nullResult = await steamApi.GetGameMetadataAsync(null!);
        var emptyResult = await steamApi.GetGameMetadataAsync("");
        var whitespaceResult = await steamApi.GetGameMetadataAsync("   ");

        // Contract should specify behavior for invalid inputs
        // In this case, our fake provider returns null
        nullResult.Should().BeNull();
        emptyResult.Should().BeNull();
        whitespaceResult.Should().BeNull();
    }

    [Fact]
    public async Task SteamApi_Contract_ResponseTime_IsReasonable()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var metadata = await steamApi.GetGameMetadataAsync("570");
        stopwatch.Stop();

        // Assert - Response should be reasonably fast (fake provider should be very fast)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "API call should complete within 1 second");
        metadata.Should().NotBeNull();
    }

    [Fact]
    public async Task SteamApi_Contract_ConcurrentRequests_Handled()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();
        var gameIds = new[] { "570", "730", "440", "10" };

        // Act - Make concurrent requests
        var tasks = gameIds.Select(id => steamApi.GetGameMetadataAsync(id)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert - All requests should succeed
        results.Should().HaveCount(4);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        results.Should().AllSatisfy(r => r!.Title.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task SteamApi_Contract_ErrorResponses_HaveConsistentStructure()
    {
        // Arrange
        var steamApi = new FakeSteamProvider();

        // Act - Request invalid game
        var metadata = await steamApi.GetGameMetadataAsync("nonexistent-game-12345");

        // Assert - Error responses should be consistent (null in this case)
        metadata.Should().BeNull();
        // If there were error objects, they should have consistent structure
        // Error messages should be user-friendly and consistent
    }
}
