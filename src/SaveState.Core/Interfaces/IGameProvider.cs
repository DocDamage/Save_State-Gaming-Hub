using SaveState.Core.Entities;

namespace SaveState.Core.Interfaces;

public interface IGameProvider
{
    string Id { get; }
    string Name { get; }

    Task<IEnumerable<Game>> GetInstalledGamesAsync();
    Task<IEnumerable<Game>> GetOwnedGamesAsync();
    Task LaunchGameAsync(Game game);
}
