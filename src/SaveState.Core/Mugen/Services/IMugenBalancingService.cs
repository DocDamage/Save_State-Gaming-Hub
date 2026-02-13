using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service interface for balancing MUGEN moves and characters.
/// Automatically adjusts move properties to maintain game balance.
/// </summary>
public interface IMugenBalancingService
{
    /// <summary>
    /// Balances a move using the specified parameters.
    /// </summary>
    /// <param name="move">The move to balance.</param>
    /// <param name="parameters">The balancing parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The balanced move definition.</returns>
    Task<Result<MugenMoveDefinition>> BalanceMoveAsync(MugenMoveDefinition move, BalanceParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Balances an entire character's moveset.
    /// </summary>
    /// <param name="characterId">The character identifier.</param>
    /// <param name="parameters">The balancing parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The balanced moveset.</returns>
    Task<Result<IReadOnlyList<MugenMoveDefinition>>> BalanceCharacterAsync(Guid characterId, BalanceParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a move for balance issues.
    /// </summary>
    /// <param name="move">The move to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Balance analysis with recommendations.</returns>
    Task<MoveBalanceAnalysis> AnalyzeMoveBalanceAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests damage values based on move properties.
    /// </summary>
    /// <param name="move">The move to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Suggested damage value.</returns>
    Task<int> SuggestDamageValueAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests frame data based on move type and damage.
    /// </summary>
    /// <param name="move">The move to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Suggested frame data.</returns>
    Task<FrameData> SuggestFrameDataAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares a move against similar moves for balance.
    /// </summary>
    /// <param name="move">The move to compare.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comparison results with similar moves.</returns>
    Task<IReadOnlyList<MoveComparison>> CompareMoveBalanceAsync(MugenMoveDefinition move, CancellationToken cancellationToken = default);
}
