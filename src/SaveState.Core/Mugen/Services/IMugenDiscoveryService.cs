namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service for discovering and installing MUGEN content.
/// </summary>
public interface IMugenDiscoveryService
{
    Task<Result<IReadOnlyList<MugenDiscoveryItem>>> SearchAsync(string query, CancellationToken ct = default);
    Task<Result> InstallAsync(MugenDiscoveryItem item, CancellationToken ct = default);
}
