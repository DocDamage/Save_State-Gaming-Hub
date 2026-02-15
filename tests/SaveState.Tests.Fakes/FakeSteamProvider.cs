using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Tests.Fakes;

public class FakeSteamProvider : IGameProvider
{
    private static readonly IReadOnlyDictionary<string, GameMetadata> _metadataById =
        new Dictionary<string, GameMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["570"] = new()
            {
                Title = "Dota 2",
                Description = "Dota 2 is a multiplayer online battle arena game developed by Valve.",
                Genres = ["Action", "MOBA", "Multi-player"],
                ReleaseDate = new DateTimeOffset(2013, 7, 9, 0, 0, 0, TimeSpan.Zero),
                Developer = "Valve",
                Publisher = "Valve",
                CoverImageUrl = "https://example.com/dota2-cover.jpg"
            },
            ["730"] = new()
            {
                Title = "Counter-Strike 2",
                Description = "Counter-Strike 2 is a team-based tactical shooter and the evolution of the CS franchise.",
                Genres = ["Action", "Shooter", "Multi-player"],
                ReleaseDate = new DateTimeOffset(2023, 9, 27, 0, 0, 0, TimeSpan.Zero),
                Developer = "Valve",
                Publisher = "Valve",
                CoverImageUrl = "https://example.com/cs2-cover.jpg"
            },
            ["440"] = new()
            {
                Title = "Team Fortress 2",
                Description = "Team Fortress 2 is a class-based multiplayer shooter developed by Valve.",
                Genres = ["Action", "Shooter", "Multi-player"],
                ReleaseDate = new DateTimeOffset(2007, 10, 10, 0, 0, 0, TimeSpan.Zero),
                Developer = "Valve",
                Publisher = "Valve",
                CoverImageUrl = "https://example.com/tf2-cover.jpg"
            },
            ["10"] = new()
            {
                Title = "Counter-Strike",
                Description = "Counter-Strike is a classic tactical shooter.",
                Genres = ["Action", "Shooter", "Multi-player"],
                ReleaseDate = new DateTimeOffset(2000, 11, 9, 0, 0, 0, TimeSpan.Zero),
                Developer = "Valve",
                Publisher = "Valve",
                CoverImageUrl = "https://example.com/cs-cover.jpg"
            }
        };

    public string Name => "Steam (Fake)";
    public ProviderCapabilities Capabilities => ProviderCapabilities.All;

    public Task<IReadOnlyList<GameInfo>> GetInstalledGamesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<GameInfo>>(new List<GameInfo>
        {
            new() {
                Title = "Half-Life 2",
                Source = "Steam",
                SourceId = "220",
                InstallPath = @"C:\Games\Half-Life 2",
                LastPlayed = DateTimeOffset.Now.AddDays(-7),
                PlayTimeMinutes = 240,
                Platform = "PC"
            },
            new() {
                Title = "Portal",
                Source = "Steam",
                SourceId = "400",
                InstallPath = @"C:\Games\Portal",
                LastPlayed = DateTimeOffset.Now.AddDays(-14),
                PlayTimeMinutes = 180,
                Platform = "PC"
            },
            new() {
                Title = "Counter-Strike 2",
                Source = "Steam",
                SourceId = "730",
                InstallPath = @"C:\Games\CS2",
                LastPlayed = DateTimeOffset.Now.AddDays(-1),
                PlayTimeMinutes = 1200,
                Platform = "PC"
            }
        });

    public Task<GameMetadata> GetGameMetadataAsync(string gameId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(gameId))
        {
            return Task.FromResult<GameMetadata>(null!);
        }

        return Task.FromResult(_metadataById.TryGetValue(gameId.Trim(), out var metadata)
            ? metadata
            : null!);
    }

    public Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
        => Task.FromResult(true);
}
