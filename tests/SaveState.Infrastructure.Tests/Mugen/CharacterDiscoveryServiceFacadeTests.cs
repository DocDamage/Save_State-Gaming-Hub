using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Mugen.CharacterDiscovery;

namespace SaveState.Infrastructure.Tests.Mugen;

public class CharacterDiscoveryServiceFacadeTests
{
    private static CharacterDiscoveryService CreateService()
    {
        return new CharacterDiscoveryService(
            NullLogger<CharacterDiscoveryService>.Instance,
            NullLoggerFactory.Instance,
            SystemTimeProvider.Instance);
    }

    [Fact]
    public async Task SearchCharactersAsync_WithNameTerm_ReturnsMatchingCharacters()
    {
        var sut = CreateService();

        var result = await sut.SearchCharactersAsync(
            new CharacterSearchQuery(SearchTerm: "Ryu", SortBy: "name", SortDescending: false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Characters.Should().NotBeEmpty();
        result.Value.Characters.Any(c => c.Name.Contains("Ryu", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task FavoritesRoundTrip_AddThenRemove_UpdatesFavoritesList()
    {
        var sut = CreateService();
        var recentResult = await sut.GetRecentlyAddedAsync(limit: 1);
        var characterId = recentResult.Value!.Single().Id;

        var addResult = await sut.AddToFavoritesAsync(characterId);
        var favoritesAfterAdd = await sut.GetFavoritesAsync();
        var removeResult = await sut.RemoveFromFavoritesAsync(characterId);
        var favoritesAfterRemove = await sut.GetFavoritesAsync();

        addResult.IsSuccess.Should().BeTrue();
        favoritesAfterAdd.IsSuccess.Should().BeTrue();
        favoritesAfterAdd.Value!.Select(c => c.Id).Should().Contain(characterId);
        removeResult.IsSuccess.Should().BeTrue();
        favoritesAfterRemove.IsSuccess.Should().BeTrue();
        favoritesAfterRemove.Value!.Select(c => c.Id).Should().NotContain(characterId);
    }

    [Fact]
    public async Task CreateCollectionAsync_ThenAddToCollection_ReturnsSuccess()
    {
        var sut = CreateService();
        var recentResult = await sut.GetRecentlyAddedAsync(limit: 1);
        var characterId = recentResult.Value!.Single().Id;

        var collectionResult = await sut.CreateCollectionAsync("My Team", "test collection");
        var addToCollectionResult = await sut.AddToCollectionAsync(collectionResult.Value!.Id, characterId);
        var collectionsResult = await sut.GetCollectionsAsync();

        collectionResult.IsSuccess.Should().BeTrue();
        addToCollectionResult.IsSuccess.Should().BeTrue();
        collectionsResult.IsSuccess.Should().BeTrue();
        collectionsResult.Value!.Select(c => c.Id).Should().Contain(collectionResult.Value.Id);
    }
}
