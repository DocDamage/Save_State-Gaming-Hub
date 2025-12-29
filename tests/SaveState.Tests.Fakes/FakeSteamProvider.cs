using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Tests.Fakes;

public class FakeSteamProvider : IGameProvider
{
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
        => Task.FromResult(new GameMetadata {
            Title = $"Game {gameId}",
            Description = "Test game description",
            Genres = new[] { "Action", "Adventure" },
            ReleaseDate = DateTimeOffset.Now.AddYears(-5)
        });

    public Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
        => Task.FromResult(true);
}
