using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service interface for testing MUGEN moves and characters.
/// Provides AI-based testing and performance analysis.
/// </summary>
public interface IMugenTestService
{
    /// <summary>
    /// Tests a move against AI opponents.
    /// </summary>
    /// <param name="move">The move to test.</param>
    /// <param name="parameters">Test parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test results with performance metrics.</returns>
    Task<Result<MoveTestResult>> TestMoveAsync(MugenMoveDefinition move, TestParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs comprehensive move analysis.
    /// </summary>
    /// <param name="move">The move to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Move analysis result.</returns>
    Task<Result<MoveTestAnalysis>> AnalyzeMoveAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests a character's complete moveset.
    /// </summary>
    /// <param name="characterId">The character identifier.</param>
    /// <param name="parameters">Test parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Test results for the character.</returns>
    Task<Result<CharacterTestResult>> TestCharacterAsync(Guid characterId, TestParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs automated balance tests on a move.
    /// </summary>
    /// <param name="move">The move to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Balance test results.</returns>
    Task<Result<BalanceTestResult>> RunBalanceTestsAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default);

    /// <summary>
    /// Simulates move performance in various scenarios.
    /// </summary>
    /// <param name="move">The move to simulate.</param>
    /// <param name="scenarioCount">Number of scenarios to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Simulation results.</returns>
    Task<Result<MoveSimulationResult>> SimulateMovePerformanceAsync(MugenMoveDefinition move, int scenarioCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets test history for a character.
    /// </summary>
    /// <param name="characterId">The character identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of historical test results.</returns>
    Task<Result<IReadOnlyList<CharacterTestResult>>> GetTestHistoryAsync(Guid characterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares test results between different versions of a move.
    /// </summary>
    /// <param name="moveId">The move identifier.</param>
    /// <param name="versionA">First version to compare.</param>
    /// <param name="versionB">Second version to compare.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comparison results.</returns>
    Task<Result<TestComparison>> CompareTestResultsAsync(Guid moveId, int versionA, int versionB, CancellationToken cancellationToken = default);
}
