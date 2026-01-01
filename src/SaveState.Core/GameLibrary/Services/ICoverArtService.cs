using SaveState.Core.Common;

namespace SaveState.Core.GameLibrary.Services;

public interface ICoverArtService
{
    Task<Result<CoverArtResult>> FetchCoverArtAsync(Guid gameId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CoverArtOption>>> SearchCoverArtAsync(string query, CancellationToken ct = default);
    Task<Result> SetCoverArtAsync(Guid gameId, string imageUrl, CancellationToken ct = default);
    Task<Result> DownloadAndCacheAsync(Guid gameId, string imageUrl, CancellationToken ct = default);
}

public sealed record CoverArtOption(
    string Url,
    string Source,
    int Width,
    int Height,
    string? Author,
    CoverArtType Type);

public sealed record CoverArtResult(
    string LocalPath,
    string SourceUrl,
    DateTime FetchedAt,
    CoverArtType Type);

public enum CoverArtType
{
    Cover,
    Banner,
    Logo,
    Icon,
    Background
}