using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;

namespace SaveState.Core.GameLibrary;

/// <summary>
/// Repository interface for managing game goals.
/// </summary>
public interface IGameGoalRepository
{
    /// <summary>
    /// Gets all goals for a specific game and user.
    /// </summary>
    Task<IReadOnlyList<GameGoal>> GetByGameIdAsync(GameId gameId, UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific goal by ID.
    /// </summary>
    Task<GameGoal?> GetByIdAsync(Guid goalId, UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active (incomplete) goals for a user.
    /// </summary>
    Task<IReadOnlyList<GameGoal>> GetActiveGoalsAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all completed goals for a user.
    /// </summary>
    Task<IReadOnlyList<GameGoal>> GetCompletedGoalsAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new goal.
    /// </summary>
    Task AddAsync(GameGoal goal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing goal.
    /// </summary>
    Task UpdateAsync(GameGoal goal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a goal.
    /// </summary>
    Task DeleteAsync(Guid goalId, CancellationToken cancellationToken = default);
}
