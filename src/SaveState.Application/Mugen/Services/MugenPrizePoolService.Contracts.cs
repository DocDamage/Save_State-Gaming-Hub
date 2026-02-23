using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// MUGEN Prize Pool service interface.
/// </summary>
public interface MugenPrizePoolServiceIMugenPrizePoolService
{
    Task<Result<MugenPrizePoolServicePrizePool>> CreatePrizePoolAsync(MugenPrizePoolServicePrizePoolCreationRequest request, CancellationToken ct = default);
    Task<Result<MugenPrizePoolServicePaymentResult>> ProcessTournamentEntryAsync(MugenPrizePoolServiceEntryPaymentRequest request, CancellationToken ct = default);
    Task<Result<MugenPrizePoolServiceSponsorContribution>> AddSponsorshipAsync(MugenPrizePoolServiceSponsorshipContributionRequest request, CancellationToken ct = default);
    Task<Result<MugenPrizePoolServicePrizeDistribution>> CalculatePrizeDistributionAsync(string prizePoolId, CancellationToken ct = default);
    Task<Result> DistributePrizesAsync(string prizePoolId, IReadOnlyList<MugenPrizePoolServiceTournamentResult> results, CancellationToken ct = default);
    Task<Result<MugenPrizePoolServicePrizePool>> GetPrizePoolAsync(string prizePoolId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MugenPrizePoolServicePrizePoolTransaction>>> GetPrizePoolTransactionsAsync(string prizePoolId, CancellationToken ct = default);
    Task<Result<MugenPrizePoolServicePrizePoolAnalytics>> GetPrizePoolAnalyticsAsync(string prizePoolId, CancellationToken ct = default);
}
