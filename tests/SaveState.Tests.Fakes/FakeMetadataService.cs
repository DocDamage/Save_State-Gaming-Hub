using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Tests.Fakes;

public class FakeMetadataService : IMetadataService
{
    private static readonly Dictionary<string, GameMetadata> _metadata = new(StringComparer.OrdinalIgnoreCase)
    {
        ["570"] = new GameMetadata
        {
            Title = "Dota 2",
            Description = "Dota 2 is a 5v5 multiplayer online battle arena game developed and published by Valve with strategic team play and hero-based combat.",
            Genres = ["Action", "MOBA", "Multi-player"],
            ReleaseDate = new DateTimeOffset(2013, 7, 9, 0, 0, 0, TimeSpan.Zero),
            Developer = "Valve",
            Publisher = "Valve",
            CoverImageUrl = "https://example.com/dota2-cover.jpg",
            UserRating = 9.1m,
            MetacriticScore = 90
        },
        ["440"] = new GameMetadata
        {
            Title = "Team Fortress 2",
            Description = "Team Fortress 2 is a class-based multiplayer shooter from Valve.",
            Genres = ["Action", "Shooter", "Multi-player"],
            ReleaseDate = new DateTimeOffset(2007, 10, 10, 0, 0, 0, TimeSpan.Zero),
            Developer = "Valve",
            Publisher = "Valve",
            CoverImageUrl = "https://example.com/tf2-cover.jpg",
            UserRating = 9.0m,
            MetacriticScore = 92
        },
        ["730"] = new GameMetadata
        {
            Title = "Counter-Strike 2",
            Description = "Counter-Strike 2 is a tactical first-person shooter focused on competitive rounds, team strategy, and precise gunplay in objective-based modes.",
            Genres = ["Action", "Shooter", "Multi-player"],
            ReleaseDate = new DateTimeOffset(2023, 9, 27, 0, 0, 0, TimeSpan.Zero),
            Developer = "Valve",
            Publisher = "Valve",
            CoverImageUrl = "https://example.com/cs2-cover.jpg",
            UserRating = 8.8m,
            MetacriticScore = 84
        },
        ["10"] = new GameMetadata
        {
            Title = "Counter-Strike",
            Description = "Counter-Strike is a pioneering tactical shooter.",
            Genres = ["Action", "Shooter", "Multi-player"],
            ReleaseDate = new DateTimeOffset(2000, 11, 9, 0, 0, 0, TimeSpan.Zero),
            Developer = "Valve",
            Publisher = "Valve",
            CoverImageUrl = "https://example.com/cs-cover.jpg",
            UserRating = 8.9m,
            MetacriticScore = 88
        },
        ["Half-Life 2"] = new GameMetadata
        {
            Title = "Half-Life 2",
            Description = "Half-Life 2 is a 2004 first-person shooter game developed by Valve.",
            Genres = new[] { "FPS", "Action", "Sci-Fi" },
            ReleaseDate = new DateTimeOffset(2004, 11, 16, 0, 0, 0, TimeSpan.Zero),
            Developer = "Valve",
            Publisher = "Valve",
            CoverImageUrl = "https://example.com/hl2-cover.jpg",
            UserRating = 9.3m,
            MetacriticScore = 96
        },
        ["Portal"] = new GameMetadata
        {
            Title = "Portal",
            Description = "Portal is a puzzle-platform video game developed by Valve.",
            Genres = new[] { "Puzzle", "Platform", "Sci-Fi" },
            ReleaseDate = new DateTimeOffset(2007, 10, 10, 0, 0, 0, TimeSpan.Zero),
            Developer = "Valve",
            Publisher = "Valve",
            CoverImageUrl = "https://example.com/portal-cover.jpg",
            UserRating = 9.0m,
            MetacriticScore = 90
        },
        ["Counter-Strike 2"] = new GameMetadata
        {
            Title = "Counter-Strike 2",
            Description = "Counter-Strike 2 is a 2012 multiplayer first-person shooter video game.",
            Genres = new[] { "FPS", "Action", "Multiplayer" },
            ReleaseDate = new DateTimeOffset(2012, 8, 21, 0, 0, 0, TimeSpan.Zero),
            Developer = "Valve",
            Publisher = "Valve",
            CoverImageUrl = "https://example.com/cs2-cover.jpg",
            UserRating = 8.5m,
            MetacriticScore = 83
        }
    };

    public Task<GameMetadata> GetGameMetadataAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Task.FromResult(GameMetadata.Empty);
        }

        _metadata.TryGetValue(title, out var metadata);
        if (metadata is not null)
        {
            return Task.FromResult(metadata);
        }

        // Steam numeric IDs should return null when not found for integration contract tests.
        if (title.All(char.IsDigit))
        {
            return Task.FromResult<GameMetadata>(null!);
        }

        return Task.FromResult(GameMetadata.Empty);
    }

    public Task<Result<byte[]>> GetCoverImageAsync(string title, CancellationToken ct = default)
    {
        // Return failure for fake implementation (no actual image data)
        return Task.FromResult(Result.Failure<byte[]>("No cover image available in fake implementation", ErrorType.NotFound));
    }
}

