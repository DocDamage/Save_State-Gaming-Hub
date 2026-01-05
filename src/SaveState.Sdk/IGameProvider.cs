using SaveState.Sdk.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Sdk;

public interface IGameProvider
{
    string Name { get; }
    Task<IReadOnlyList<GameInfo>> GetInstalledGamesAsync(CancellationToken ct = default);
}
