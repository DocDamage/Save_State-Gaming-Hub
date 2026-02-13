using SaveState.Core.Common;
using SaveState.Application.Mugen.Models.BalanceTuning;

namespace SaveState.Application.Mugen.Services.BalanceTuning;

/// <summary>
/// Balance tuning service interface for competitive balance of advanced mechanics.
/// </summary>
public interface IBalanceTuningService
{
    Task<Result<BalanceAnalysis>> AnalyzeBalanceAsync(string sessionId, IReadOnlyList<MatchData> matchData, CancellationToken ct = default);
    Task<Result<BalanceAdjustment>> CalculateAdjustmentAsync(MechanicType mechanic, BalanceData balanceData, CancellationToken ct = default);
    Task<Result<AdjustmentApplication>> ApplyAdjustmentAsync(BalanceAdjustment adjustment, CancellationToken ct = default);
    Task<Result<BalancePatch>> CreateBalancePatchAsync(IReadOnlyList<BalanceAdjustment> adjustments, string patchVersion, CancellationToken ct = default);
    Task<Result<BalanceMonitoring>> MonitorBalanceAsync(string sessionId, CancellationToken ct = default);
    Task<Result<CompetitiveRanking>> UpdateCompetitiveRankingAsync(IReadOnlyList<PlayerStats> playerStats, CancellationToken ct = default);
    Task<Result<BalanceReport>> GenerateBalanceReportAsync(string sessionId, DateRange dateRange, CancellationToken ct = default);
}
