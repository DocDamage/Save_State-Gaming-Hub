using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary.DomainServices;

public interface IGameValidationService
{
    Task<bool> IsValidGameAsync(Game game, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetValidationErrorsAsync(Game game, CancellationToken ct = default);
    Task<bool> CanLaunchGameAsync(Game game, CancellationToken ct = default);
}
