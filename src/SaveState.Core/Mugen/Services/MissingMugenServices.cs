using SaveState.Core.Common;
using SaveState.Core.Mugen.DTOs;
using SaveState.Core.Mugen.Entities;

namespace SaveState.Core.Mugen.Services;

public interface IMugenMoveListService
{
    Task<Result<IEnumerable<MugenMoveEntryDto>>> GetMoveListAsync(MugenCharacter character, CancellationToken ct = default);
}

public interface IMugenAssetPreviewService
{
    Task<Result<IEnumerable<MugenAssetEntry>>> GetAssetsAsync(MugenCharacter character, CancellationToken ct = default);
}

public interface IMugenCompatibilityService
{
    Task<Result<CompatibilityAnalysisResult>> AnalyzeAsync(MugenCharacter character, CancellationToken ct = default);
    Task<Result<CompatibilityFixResult>> FixAsync(MugenCharacter character, CancellationToken ct = default);
}

public interface IMugenNetplayService
{
    Task<Result<IEnumerable<MugenNetplayLobby>>> GetLobbiesAsync(CancellationToken ct = default);
    Task<Result> JoinLobbyAsync(MugenNetplayLobby lobby, CancellationToken ct = default);
}

public interface IMugenEloService
{
    Task<Result<IEnumerable<MugenEloRating>>> GetRatingsAsync(CancellationToken ct = default);
}

public interface IMugenConfigService
{
    Task<Result> UpdateConfigAsync(string section, string key, string value, CancellationToken ct = default);
    Task<Result<string>> GetConfigValueAsync(string section, string key, CancellationToken ct = default);
}

public interface IMugenDiscoveryService
{
    Task<Result<IEnumerable<MugenDiscoveryItem>>> SearchAsync(string query, CancellationToken ct = default);
    Task<Result<IEnumerable<MugenDiscoveryItem>>> GetFeaturedAsync(CancellationToken ct = default);
    Task<Result> InstallAsync(MugenDiscoveryItem item, CancellationToken ct = default);
}
