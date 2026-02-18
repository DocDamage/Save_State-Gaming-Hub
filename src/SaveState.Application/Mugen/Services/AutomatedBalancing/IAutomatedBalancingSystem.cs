namespace SaveState.Application.Mugen.Services.AutomatedBalancing;

/// <summary>
/// Interface for automated game balance system.
/// </summary>
public interface IAutomatedBalancingSystem
{
    /// <summary>
    /// Analyzes current game balance.
    /// </summary>
    Task<BalanceAnalysis> AnalyzeBalanceAsync(CancellationToken ct = default);

    /// <summary>
    /// Generates balance adjustments.
    /// </summary>
    Task<IReadOnlyList<BalanceAdjustment>> GenerateAdjustmentsAsync(BalanceAnalysis analysis, CancellationToken ct = default);

    /// <summary>
    /// Applies a balance patch.
    /// </summary>
    Task<BalancePatch> ApplyPatchAsync(BalanceAdjustment adjustment, CancellationToken ct = default);

    /// <summary>
    /// Gets balance report for a game.
    /// </summary>
    Task<GameBalanceReport> GetBalanceReportAsync(string gameId, CancellationToken ct = default);
}