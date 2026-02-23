using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Professional prize pool management for esports tournaments.
/// Handles entry fees, prize distribution, sponsorships, and financial operations.
/// </summary>
public class MugenPrizePoolService : MugenPrizePoolServiceIMugenPrizePoolService
{
    private readonly ILogger<MugenPrizePoolService> _logger;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, MugenPrizePoolServicePrizePool> _activePrizePools = new();
    private readonly Dictionary<string, MugenPrizePoolServiceTournamentEntry> _tournamentEntries = new();
    private readonly MugenPrizePoolServicePaymentProcessor _paymentProcessor;
    private readonly MugenPrizePoolServicePrizeDistributionEngine _distributionEngine;

    public MugenPrizePoolService(
        ILogger<MugenPrizePoolService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _cache = cache;
        _timeProvider = timeProvider;
        _paymentProcessor = new MugenPrizePoolServicePaymentProcessor(loggerFactory.CreateLogger<MugenPrizePoolServicePaymentProcessor>(), timeProvider);
        _distributionEngine = new MugenPrizePoolServicePrizeDistributionEngine(loggerFactory.CreateLogger<MugenPrizePoolServicePrizeDistributionEngine>(), timeProvider);
    }

    public async Task<Result<MugenPrizePoolServicePrizePool>> CreatePrizePoolAsync(MugenPrizePoolServicePrizePoolCreationRequest request, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            _logger.LogInformation("Creating prize pool for tournament {TournamentId}", request.TournamentId);

            var prizePool = new MugenPrizePoolServicePrizePool
            {
                PrizePoolId = Guid.NewGuid().ToString(),
                TournamentId = request.TournamentId,
                TotalPool = 0,
                EntryFee = request.EntryFee,
                MaxParticipants = request.MaxParticipants,
                CurrentParticipants = 0,
                GuaranteedPrize = request.GuaranteedPrize,
                HousePercentage = request.HousePercentage,
                MugenPrizePoolServicePrizeDistribution = request.MugenPrizePoolServicePrizeDistribution,
                Sponsors = new List<MugenPrizePoolServiceSponsorContribution>(),
                Status = MugenPrizePoolServicePrizePoolStatus.Open,
                CreatedAt = _timeProvider.UtcNow,
                EntryDeadline = request.EntryDeadline,
                Funds = new MugenPrizePoolServicePrizePoolFunds
                {
                    EntryFees = 0,
                    Sponsorships = 0,
                    HouseContribution = 0,
                    TotalAvailable = 0
                },
                Transactions = new List<MugenPrizePoolServicePrizePoolTransaction>()
            };

            // Add guaranteed prize as house contribution
            if (request.GuaranteedPrize > 0)
            {
                prizePool.Funds.HouseContribution = request.GuaranteedPrize;
                prizePool.Funds.TotalAvailable = request.GuaranteedPrize;

                var transaction = new MugenPrizePoolServicePrizePoolTransaction
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Type = MugenPrizePoolServiceTransactionType.HouseContribution,
                    Amount = request.GuaranteedPrize,
                    Description = "Guaranteed prize pool contribution",
                    Timestamp = _timeProvider.UtcNow,
                    Processed = true
                };

                prizePool.Transactions.Add(transaction);
            }

            _activePrizePools[prizePool.PrizePoolId] = prizePool;

            _logger.LogInformation("Prize pool created: {PrizePoolId}", prizePool.PrizePoolId);
            return Result.Success<MugenPrizePoolServicePrizePool>(prizePool);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prize pool for tournament {TournamentId}", request.TournamentId);
            return Result.Failure<MugenPrizePoolServicePrizePool>($"Failed to create prize pool: {ex.Message}");
        }
    }

    public async Task<Result<MugenPrizePoolServicePaymentResult>> ProcessTournamentEntryAsync(MugenPrizePoolServiceEntryPaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing tournament entry for player {PlayerId}", request.PlayerId);

            if (!_activePrizePools.TryGetValue(request.PrizePoolId, out var prizePool))
            {
                return Result.Failure<MugenPrizePoolServicePaymentResult>("Prize pool not found");
            }

            if (prizePool.Status != MugenPrizePoolServicePrizePoolStatus.Open)
            {
                return Result.Failure<MugenPrizePoolServicePaymentResult>("Tournament entry is not open");
            }

            if (prizePool.CurrentParticipants >= prizePool.MaxParticipants)
            {
                return Result.Failure<MugenPrizePoolServicePaymentResult>("Tournament is at maximum capacity");
            }

            // Check if player already entered
            var existingEntry = _tournamentEntries.Values.FirstOrDefault(e =>
                e.PrizePoolId == request.PrizePoolId && e.PlayerId == request.PlayerId);

            if (existingEntry != null)
            {
                return Result.Failure<MugenPrizePoolServicePaymentResult>("Player has already entered this tournament");
            }

            // Process payment
            var paymentResult = await _paymentProcessor.ProcessPaymentAsync(
                request.MugenPrizePoolServicePaymentMethod,
                request.PlayerId,
                prizePool.EntryFee,
                $"Tournament entry - {prizePool.TournamentId}",
                ct);

            if (!paymentResult.IsSuccess || paymentResult.Value is null)
            {
                return Result.Failure<MugenPrizePoolServicePaymentResult>(
                    $"Payment failed: {paymentResult.Error ?? "Unknown error"}");
            }

            var paymentValue = paymentResult.Value;

            // Record entry
            var entry = new MugenPrizePoolServiceTournamentEntry
            {
                EntryId = Guid.NewGuid().ToString(),
                PrizePoolId = request.PrizePoolId,
                PlayerId = request.PlayerId,
                EntryFee = prizePool.EntryFee,
                EntryTime = _timeProvider.UtcNow,
                PaymentId = paymentValue.TransactionId,
                Status = MugenPrizePoolServiceEntryStatus.Confirmed
            };

            _tournamentEntries[entry.EntryId] = entry;
            prizePool.CurrentParticipants++;
            prizePool.Funds.EntryFees += prizePool.EntryFee;
            prizePool.Funds.TotalAvailable += prizePool.EntryFee;

            // Record transaction
            var transaction = new MugenPrizePoolServicePrizePoolTransaction
            {
                TransactionId = paymentValue.TransactionId,
                Type = MugenPrizePoolServiceTransactionType.EntryFee,
                Amount = prizePool.EntryFee,
                Description = $"Entry fee from {request.PlayerId}",
                Timestamp = _timeProvider.UtcNow,
                Processed = true,
                PlayerId = request.PlayerId
            };

            prizePool.Transactions.Add(transaction);

            await UpdatePrizePoolCacheAsync(prizePool, ct);

            _logger.LogInformation("Tournament entry processed for player {PlayerId}", request.PlayerId);
            return Result.Success<MugenPrizePoolServicePaymentResult>(paymentValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing tournament entry for {PlayerId}", request.PlayerId);
            return Result.Failure<MugenPrizePoolServicePaymentResult>($"Failed to process entry: {ex.Message}");
        }
    }

    public async Task<Result<MugenPrizePoolServiceSponsorContribution>> AddSponsorshipAsync(MugenPrizePoolServiceSponsorshipContributionRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Adding sponsorship from {SponsorName} to prize pool {PrizePoolId}",
                request.SponsorName, request.PrizePoolId);

            if (!_activePrizePools.TryGetValue(request.PrizePoolId, out var prizePool))
            {
                return Result.Failure<MugenPrizePoolServiceSponsorContribution>("Prize pool not found");
            }

            // Process sponsorship payment
            var paymentResult = await _paymentProcessor.ProcessPaymentAsync(
                request.MugenPrizePoolServicePaymentMethod,
                request.SponsorId,
                request.Amount,
                $"Sponsorship contribution - {prizePool.TournamentId}",
                ct);

            if (!paymentResult.IsSuccess || paymentResult.Value is null)
            {
                return Result.Failure<MugenPrizePoolServiceSponsorContribution>(
                    $"Payment failed: {paymentResult.Error ?? "Unknown error"}");
            }

            var paymentValue = paymentResult.Value;

            // Add sponsorship
            var contribution = new MugenPrizePoolServiceSponsorContribution
            {
                SponsorId = request.SponsorId,
                SponsorName = request.SponsorName,
                Amount = request.Amount,
                AgreedAt = _timeProvider.UtcNow,
                PaymentId = paymentValue.TransactionId,
                ContributionDate = _timeProvider.UtcNow,
                LogoUrl = request.LogoUrl,
                WebsiteUrl = request.WebsiteUrl,
                VisibilityLevel = request.VisibilityLevel
            };

            prizePool.Sponsors.Add(contribution);
            prizePool.Funds.Sponsorships += request.Amount;
            prizePool.Funds.TotalAvailable += request.Amount;

            // Record transaction
            var transaction = new MugenPrizePoolServicePrizePoolTransaction
            {
                TransactionId = paymentValue.TransactionId,
                Type = MugenPrizePoolServiceTransactionType.Sponsorship,
                Amount = request.Amount,
                Description = $"Sponsorship from {request.SponsorName}",
                Timestamp = _timeProvider.UtcNow,
                Processed = true,
                SponsorId = request.SponsorId
            };

            prizePool.Transactions.Add(transaction);

            await UpdatePrizePoolCacheAsync(prizePool, ct);

            _logger.LogInformation("Sponsorship added: {Amount} from {SponsorName}", request.Amount, request.SponsorName);
            return Result.Success<MugenPrizePoolServiceSponsorContribution>(contribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding sponsorship from {SponsorName}", request.SponsorName);
            return Result.Failure<MugenPrizePoolServiceSponsorContribution>($"Failed to add sponsorship: {ex.Message}");
        }
    }

    public async Task<Result<MugenPrizePoolServicePrizeDistribution>> CalculatePrizeDistributionAsync(string prizePoolId, CancellationToken ct = default)
    {
        try
        {
            if (!_activePrizePools.TryGetValue(prizePoolId, out var prizePool))
            {
                return Result.Failure<MugenPrizePoolServicePrizeDistribution>("Prize pool not found");
            }

            return await _distributionEngine.CalculateDistributionAsync(prizePool, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating prize distribution for {PrizePoolId}", prizePoolId);
            return Result.Failure<MugenPrizePoolServicePrizeDistribution>($"Failed to calculate distribution: {ex.Message}");
        }
    }

    public async Task<Result> DistributePrizesAsync(string prizePoolId, IReadOnlyList<MugenPrizePoolServiceTournamentResult> results, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Distributing prizes for prize pool {PrizePoolId}", prizePoolId);

            if (!_activePrizePools.TryGetValue(prizePoolId, out var prizePool))
            {
                return Result.Failure("Prize pool not found");
            }

            if (prizePool.Status != MugenPrizePoolServicePrizePoolStatus.Open)
            {
                return Result.Failure("Prize pool is not in a distributable state");
            }

            // Calculate final prize distribution
            var distribution = await _distributionEngine.CalculateDistributionAsync(prizePool, ct);
            if (!distribution.IsSuccess || distribution.Value is null)
            {
                return Result.Failure($"Failed to calculate distribution: {distribution.Error ?? "Unknown error"}");
            }

            var distributionValue = distribution.Value;

            // Distribute prizes to winners
            foreach (var result in results)
            {
                var prizeAmount = distributionValue.GetPrizeForPlacement(result.Placement);
                if (prizeAmount > 0)
                {
                    var payoutResult = await _paymentProcessor.ProcessPayoutAsync(
                        result.PlayerId,
                        prizeAmount,
                        $"Tournament prize - {result.Placement} place",
                        ct);

                    if (!payoutResult.IsSuccess)
                    {
                        _logger.LogWarning("Failed to payout {Amount} to {PlayerId}: {Error}",
                            prizeAmount, result.PlayerId, payoutResult.Error);
                        continue;
                    }

                    // Record transaction
                    var transaction = new MugenPrizePoolServicePrizePoolTransaction
                    {
                        TransactionId = Guid.NewGuid().ToString(),
                        Type = MugenPrizePoolServiceTransactionType.PrizePayout,
                        Amount = -prizeAmount, // Negative for payouts
                        Description = $"{result.Placement} place prize to {result.PlayerId}",
                        Timestamp = _timeProvider.UtcNow,
                        Processed = true,
                        PlayerId = result.PlayerId
                    };

                    prizePool.Transactions.Add(transaction);
                }
            }

            // Close prize pool
            prizePool.Status = MugenPrizePoolServicePrizePoolStatus.Distributed;
            prizePool.DistributedAt = _timeProvider.UtcNow;

            await UpdatePrizePoolCacheAsync(prizePool, ct);

            _logger.LogInformation("Prizes distributed for prize pool {PrizePoolId}", prizePoolId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error distributing prizes for {PrizePoolId}", prizePoolId);
            return Result.Failure($"Failed to distribute prizes: {ex.Message}");
        }
    }

    public async Task<Result<MugenPrizePoolServicePrizePool>> GetPrizePoolAsync(string prizePoolId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            if (!_activePrizePools.TryGetValue(prizePoolId, out var prizePool))
            {
                return Result.Failure<MugenPrizePoolServicePrizePool>("Prize pool not found");
            }

            return Result.Success<MugenPrizePoolServicePrizePool>(prizePool);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prize pool {PrizePoolId}", prizePoolId);
            return Result.Failure<MugenPrizePoolServicePrizePool>($"Failed to get prize pool: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<MugenPrizePoolServicePrizePoolTransaction>>> GetPrizePoolTransactionsAsync(string prizePoolId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            if (!_activePrizePools.TryGetValue(prizePoolId, out var prizePool))
            {
                return Result.Failure<IReadOnlyList<MugenPrizePoolServicePrizePoolTransaction>>("Prize pool not found");
            }

            return Result.Success<IReadOnlyList<MugenPrizePoolServicePrizePoolTransaction>>(prizePool.Transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions for prize pool {PrizePoolId}", prizePoolId);
            return Result.Failure<IReadOnlyList<MugenPrizePoolServicePrizePoolTransaction>>($"Failed to get transactions: {ex.Message}");
        }
    }

    public async Task<Result<MugenPrizePoolServicePrizePoolAnalytics>> GetPrizePoolAnalyticsAsync(string prizePoolId, CancellationToken ct = default)
    {
        try
        {
            if (!_activePrizePools.TryGetValue(prizePoolId, out var prizePool))
            {
                return Result.Failure<MugenPrizePoolServicePrizePoolAnalytics>("Prize pool not found");
            }

            var analytics = new MugenPrizePoolServicePrizePoolAnalytics
            {
                PrizePoolId = prizePoolId,
                TotalPool = prizePool.Funds.TotalAvailable,
                EntryFeesCollected = prizePool.Funds.EntryFees,
                SponsorshipRevenue = prizePool.Funds.Sponsorships,
                HouseContribution = prizePool.Funds.HouseContribution,
                ParticipantsCount = prizePool.CurrentParticipants,
                CapacityUtilization = (double)prizePool.CurrentParticipants / prizePool.MaxParticipants,
                AverageEntryFee = prizePool.EntryFee,
                ProjectedPayout = await CalculateProjectedPayoutAsync(prizePool, ct),
                TransactionCount = prizePool.Transactions.Count,
                LastActivity = prizePool.Transactions.Any() ? prizePool.Transactions.Max(t => t.Timestamp) : prizePool.CreatedAt
            };

            return Result.Success<MugenPrizePoolServicePrizePoolAnalytics>(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics for prize pool {PrizePoolId}", prizePoolId);
            return Result.Failure<MugenPrizePoolServicePrizePoolAnalytics>($"Failed to get analytics: {ex.Message}");
        }
    }

    #region Private Methods

    private async Task UpdatePrizePoolCacheAsync(MugenPrizePoolServicePrizePool prizePool, CancellationToken ct)
    {
        var cacheKey = $"prize_pool_{prizePool.PrizePoolId}";
        await _cache.SetAsync(cacheKey, prizePool, TimeSpan.FromHours(1), ct);
    }

    private async Task<decimal> CalculateProjectedPayoutAsync(MugenPrizePoolServicePrizePool prizePool, CancellationToken ct)
    {
        var distribution = await _distributionEngine.CalculateDistributionAsync(prizePool, ct);
        if (!distribution.IsSuccess || distribution.Value is null)
        {
            return 0;
        }

        return distribution.Value.TotalPayout;
    }

    #endregion
}
