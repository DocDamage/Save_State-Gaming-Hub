namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Service for discovering IKEMEN netplay lobbies.
/// </summary>
public interface IMugenNetplayService
{
    Task<Result<IReadOnlyList<MugenNetplayLobby>>> GetLobbiesAsync(CancellationToken ct = default);
    Task<Result> JoinLobbyAsync(MugenNetplayLobby lobby, CancellationToken ct = default);
}
