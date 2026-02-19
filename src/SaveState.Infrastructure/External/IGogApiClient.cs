using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.External;

public interface IGogApiClient
{
    Task<Result<IReadOnlyList<GogGame>>> GetOwnedGamesAsync(CancellationToken ct = default);
    Task<Result<GameMetadata>> GetGameDetailsAsync(string gameId, CancellationToken ct = default);
    Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default);
}

public class GogGame
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? InstallPath { get; set; }
    public DateTimeOffset? LastPlayedDate { get; set; }
    public int? PlayTimeMinutes { get; set; }
}

public class GogApiException : Exception
{
    public GogApiException(string message) : base(message) { }
    public GogApiException(string message, Exception innerException) : base(message, innerException) { }
}
