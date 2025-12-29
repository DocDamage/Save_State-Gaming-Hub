using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.External;

public interface IEpicApiClient
{
    Task<IReadOnlyList<EpicGame>> GetOwnedGamesAsync(CancellationToken ct = default);
    Task<GameMetadata> GetGameDetailsAsync(string gameId, CancellationToken ct = default);
    Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default);
}

public class EpicGame
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? InstallPath { get; set; }
    public DateTimeOffset? LastPlayedDate { get; set; }
    public int? PlayTimeMinutes { get; set; }
}

public class EpicApiException : Exception
{
    public EpicApiException(string message) : base(message) { }
    public EpicApiException(string message, Exception innerException) : base(message, innerException) { }
}
