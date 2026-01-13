using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Tests.Fakes;

public class FakeMetadataService : IMetadataService
{
    private static readonly Dictionary<string, GameMetadata> _metadata = new(StringComparer.OrdinalIgnoreCase)
    {
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
        _metadata.TryGetValue(title, out var metadata);
        return Task.FromResult(metadata ?? GameMetadata.Empty);
    }

    public Task<Result<byte[]>> GetCoverImageAsync(string title, CancellationToken ct = default)
    {
        // Return failure for fake implementation (no actual image data)
        return Task.FromResult(Result.Failure<byte[]>("No cover image available in fake implementation", ErrorType.NotFound));
    }
}

