using SaveState.Core.Common;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.External;

public interface IIgdbApiClient
{
    Task<IReadOnlyList<IgdbGame>> SearchGamesAsync(string title, CancellationToken ct = default);
    Task<GameMetadata> GetGameDetailsAsync(string gameId, CancellationToken ct = default);
    Task<Result<byte[]>> DownloadImageAsync(string imageUrl, CancellationToken ct = default);
}

public class IgdbGame
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTimeOffset? FirstReleaseDate { get; set; }
    public IgdbGenre[] Genres { get; set; } = Array.Empty<IgdbGenre>();
    public IgdbCover? Cover { get; set; }
}

public class IgdbGenre
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class IgdbCover
{
    public string? Url { get; set; }
}

public class IgdbApiException : Exception
{
    public IgdbApiException(string message) : base(message) { }
    public IgdbApiException(string message, Exception innerException) : base(message, innerException) { }
}
