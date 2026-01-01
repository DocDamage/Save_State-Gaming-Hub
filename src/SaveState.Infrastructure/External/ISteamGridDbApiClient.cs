using SaveState.Core.Common;

namespace SaveState.Infrastructure.External;

public interface ISteamGridDbApiClient
{
    Task<IReadOnlyList<SteamGridDbGrid>> SearchGridsAsync(string query, CancellationToken ct = default);
    Task<IReadOnlyList<SteamGridDbGrid>> GetGridsBySteamIdAsync(int steamId, CancellationToken ct = default);
    Task<Result<byte[]>> DownloadImageAsync(string imageUrl, CancellationToken ct = default);
}

public class SteamGridDbGrid
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Thumb { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string Style { get; set; } = string.Empty; // "grid", "hero", "logo", etc.
    public SteamGridDbAuthor? Author { get; set; }
    public int Score { get; set; }
}

public class SteamGridDbAuthor
{
    public string Name { get; set; } = string.Empty;
    public string SteamId { get; set; } = string.Empty;
}

public class SteamGridDbApiException : Exception
{
    public SteamGridDbApiException(string message) : base(message) { }
    public SteamGridDbApiException(string message, Exception innerException) : base(message, innerException) { }
}