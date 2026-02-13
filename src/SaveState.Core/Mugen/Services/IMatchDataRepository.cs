namespace SaveState.Core.Mugen.Services;

using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Interface for repository storing and retrieving match data for ML training.
/// </summary>
public interface IMatchDataRepository
{
    /// <summary>
    /// Gets recent matches for analysis.
    /// </summary>
    /// <param name="count">Number of matches to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of recent matches.</returns>
    Task<Result<IReadOnlyList<MLMatchResult>>> GetRecentMatchesAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Saves a match result for training data.
    /// </summary>
    /// <param name="result">The match result to save.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<Result> SaveMatchResultAsync(MLMatchResult result, CancellationToken ct = default);
}
