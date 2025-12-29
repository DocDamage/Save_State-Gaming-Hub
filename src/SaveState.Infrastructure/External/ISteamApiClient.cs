using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.External;

public interface ISteamApiClient
{
    Task<IReadOnlyList<SteamGame>> GetOwnedGamesAsync(CancellationToken ct = default);
    Task<GameMetadata> GetGameDetailsAsync(string appId, CancellationToken ct = default);
    Task<bool> LaunchGameAsync(string appId, CancellationToken ct = default);
}

public class SteamGame
{
    public int AppId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? InstallPath { get; set; }
    public DateTimeOffset? LastPlayedDate { get; set; }
    public int? PlayTimeMinutes { get; set; }
}

public class SteamApiException : Exception
{
    public SteamApiException(string message) : base(message) { }
    public SteamApiException(string message, Exception innerException) : base(message, innerException) { }
}
