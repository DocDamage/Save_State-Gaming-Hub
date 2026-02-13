using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Professional prize pool management for esports tournaments.
/// Handles entry fees, prize distribution, sponsorships, and financial operations.
/// </summary>
public class MugenPrizePoolService : MugenPrizePoolServiceIMugenPrizePoolService
{
    private readonly ILogger<MugenPrizePoolService> _logger;
    private readonly ICacheService _cache;
    private readonly Dictionary<string, MugenPrizePoolServicePrizePool> _activePrizePools = new();
    private readonly Dictionary<string, MugenPrizePoolServiceTournamentEntry> _tournamentEntries = new();
    private readonly MugenPrizePoolServicePaymentProcessor _paymentProcessor;
    private readonly MugenPrizePoolServicePrizeDistributionEngine _distributionEngine;

    public MugenPrizePoolService(
        ILogger<MugenPrizePoolService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache)
    {
        _logger = logger;
        _cache = cache;
        _paymentProcessor = new MugenPrizePoolServicePaymentProcessor(loggerFactory.CreateLogger<MugenPrizePoolServicePaymentProcessor>());
        _distributionEngine = new MugenPrizePoolServicePrizeDistributionEngine(loggerFactory.CreateLogger<MugenPrizePoolServicePrizeDistributionEngine>());
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
                CreatedAt = DateTime.UtcNow,
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
                    Timestamp = DateTime.UtcNow,
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
                EntryTime = DateTime.UtcNow,
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
                Timestamp = DateTime.UtcNow,
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
                AgreedAt = DateTime.UtcNow,
                PaymentId = paymentValue.TransactionId,
                ContributionDate = DateTime.UtcNow,
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
                Timestamp = DateTime.UtcNow,
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
                        Timestamp = DateTime.UtcNow,
                        Processed = true,
                        PlayerId = result.PlayerId
                    };

                    prizePool.Transactions.Add(transaction);
                }
            }

            // Close prize pool
            prizePool.Status = MugenPrizePoolServicePrizePoolStatus.Distributed;
            prizePool.DistributedAt = DateTime.UtcNow;

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

/// <summary>
/// Payment processor for handling tournament entries and prizes.
/// </summary>
public class MugenPrizePoolServicePaymentProcessor
{
    private readonly ILogger<MugenPrizePoolServicePaymentProcessor> _logger;
    private readonly Dictionary<string, MugenPrizePoolServicePaymentTransaction> _transactions = new();

    public MugenPrizePoolServicePaymentProcessor(ILogger<MugenPrizePoolServicePaymentProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<Result<MugenPrizePoolServicePaymentResult>> ProcessPaymentAsync(
        MugenPrizePoolServicePaymentMethod method,
        string payerId,
        decimal amount,
        string description,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing payment of {Amount} from {PayerId}", amount, payerId);

            // Simulate payment processing
            await Task.Delay(500, ct); // Simulate processing time

            var transactionId = Guid.NewGuid().ToString();
            var transaction = new MugenPrizePoolServicePaymentTransaction
            {
                TransactionId = transactionId,
                PayerId = payerId,
                Amount = amount,
                Method = method,
                Description = description,
                Timestamp = DateTime.UtcNow,
                Status = MugenPrizePoolServicePaymentStatus.Completed
            };

            _transactions[transactionId] = transaction;

            var result = new MugenPrizePoolServicePaymentResult
            {
                TransactionId = transactionId,
                Amount = amount,
                Status = MugenPrizePoolServicePaymentStatus.Completed,
                ProcessedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Payment processed: {TransactionId}", transactionId);
            return Result.Success<MugenPrizePoolServicePaymentResult>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment from {PayerId}", payerId);
            return Result.Failure<MugenPrizePoolServicePaymentResult>($"Payment processing failed: {ex.Message}");
        }
    }

    public async Task<Result<MugenPrizePoolServicePayoutResult>> ProcessPayoutAsync(
        string recipientId,
        decimal amount,
        string description,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Processing payout of {Amount} to {RecipientId}", amount, recipientId);

            // Simulate payout processing
            await Task.Delay(300, ct);

            var result = new MugenPrizePoolServicePayoutResult
            {
                RecipientId = recipientId,
                Amount = amount,
                Status = MugenPrizePoolServicePayoutStatus.Completed,
                ProcessedAt = DateTime.UtcNow,
                TransactionId = Guid.NewGuid().ToString()
            };

            _logger.LogInformation("Payout processed: {TransactionId}", result.TransactionId);
            return Result.Success<MugenPrizePoolServicePayoutResult>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payout to {RecipientId}", recipientId);
            return Result.Failure<MugenPrizePoolServicePayoutResult>($"Payout processing failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Prize distribution engine for calculating tournament payouts.
/// </summary>
public class MugenPrizePoolServicePrizeDistributionEngine
{
    private readonly ILogger<MugenPrizePoolServicePrizeDistributionEngine> _logger;

    public MugenPrizePoolServicePrizeDistributionEngine(ILogger<MugenPrizePoolServicePrizeDistributionEngine> logger)
    {
        _logger = logger;
    }

    public async Task<Result<MugenPrizePoolServicePrizeDistribution>> CalculateDistributionAsync(MugenPrizePoolServicePrizePool prizePool, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            var distribution = new MugenPrizePoolServicePrizeDistribution
            {
                PrizePoolId = prizePool.PrizePoolId,
                TotalPool = prizePool.Funds.TotalAvailable,
                HouseCut = prizePool.Funds.TotalAvailable * prizePool.HousePercentage,
                Prizes = new List<MugenPrizePoolServicePrizeTier>(),
                CalculatedAt = DateTime.UtcNow
            };

            var distributableAmount = prizePool.Funds.TotalAvailable - distribution.HouseCut;
            var prizeTiers = CalculatePrizeTiers(prizePool.MugenPrizePoolServicePrizeDistribution, distributableAmount, prizePool.MaxParticipants);

            distribution.Prizes = prizeTiers;
            // distribution.TotalPayout is a calculated property, no need to assign


            return Result.Success<MugenPrizePoolServicePrizeDistribution>(distribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating prize distribution");
            return Result.Failure<MugenPrizePoolServicePrizeDistribution>($"Distribution calculation failed: {ex.Message}");
        }
    }

    private IReadOnlyList<MugenPrizePoolServicePrizeTier> CalculatePrizeTiers(MugenPrizePoolServicePrizeDistributionRules rules, decimal totalAmount, int maxParticipants)
    {
        var tiers = new List<MugenPrizePoolServicePrizeTier>();
        var participants = Math.Min(maxParticipants, 8); // Assume top 8 get prizes

        // Standard tournament distribution: 50%, 25%, 15%, 10% for top 4, then smaller amounts
        var distribution = new[] { 0.50m, 0.25m, 0.15m, 0.10m, 0.05m, 0.03m, 0.02m, 0.01m };

        for (int i = 0; i < Math.Min(distribution.Length, participants); i++)
        {
            var amount = totalAmount * distribution[i];
            tiers.Add(new MugenPrizePoolServicePrizeTier
            {
                Placement = i + 1,
                Amount = Math.Round(amount, 2),
                Percentage = distribution[i]
            });
        }

        return tiers;
    }
}

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

/// <summary>
/// Prize pool data.
/// </summary>
public class MugenPrizePoolServicePrizePool
{
    public string PrizePoolId { get; set; } = default!;
    public string TournamentId { get; set; } = default!;
    public decimal TotalPool { get; set; } = default!;
    public decimal EntryFee { get; set; } = default!;
    public int MaxParticipants { get; set; } = default!;
    public int CurrentParticipants { get; set; } = default!;
    public decimal GuaranteedPrize { get; set; } = default!;
    public decimal HousePercentage { get; set; } = default!;
    public MugenPrizePoolServicePrizeDistributionRules MugenPrizePoolServicePrizeDistribution { get; set; } = default!;
    public List<MugenPrizePoolServiceSponsorContribution> Sponsors { get; set; } = default!;
    public MugenPrizePoolServicePrizePoolStatus Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime? EntryDeadline { get; set; } = default!;
    public MugenPrizePoolServicePrizePoolFunds Funds { get; set; } = default!;
    public List<MugenPrizePoolServicePrizePoolTransaction> Transactions { get; set; } = default!;
    public DateTime? DistributedAt { get; set; } = default!;
}

/// <summary>
/// Prize pool status enumeration.
/// </summary>
public enum MugenPrizePoolServicePrizePoolStatus
{
    Open,
    Closed,
    Distributing,
    Distributed
}

/// <summary>
/// Prize pool funds.
/// </summary>
public class MugenPrizePoolServicePrizePoolFunds
{
    public decimal EntryFees { get; set; } = default!;
    public decimal Sponsorships { get; set; } = default!;
    public decimal HouseContribution { get; set; } = default!;
    public decimal TotalAvailable { get; set; } = default!;
}

/// <summary>
/// Prize distribution rules.
/// </summary>
public class MugenPrizePoolServicePrizeDistributionRules
{
    public string DistributionType { get; set; } = default!;
    public IReadOnlyDictionary<int, decimal>? CustomDistribution { get; set; } = default!;
}

/// <summary>
/// Prize pool creation request.
/// </summary>
public class MugenPrizePoolServicePrizePoolCreationRequest
{
    public string TournamentId { get; set; } = default!;
    public decimal EntryFee { get; set; } = default!;
    public int MaxParticipants { get; set; } = default!;
    public decimal GuaranteedPrize { get; set; } = default!;
    public decimal HousePercentage { get; set; } = default!;
    public MugenPrizePoolServicePrizeDistributionRules MugenPrizePoolServicePrizeDistribution { get; set; } = default!;
    public DateTime? EntryDeadline { get; set; } = default!;
}

/// <summary>
/// Entry payment request.
/// </summary>
public class MugenPrizePoolServiceEntryPaymentRequest
{
    public string PrizePoolId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public MugenPrizePoolServicePaymentMethod MugenPrizePoolServicePaymentMethod { get; set; } = default!;
}

/// <summary>
/// Payment method enumeration.
/// </summary>
public enum MugenPrizePoolServicePaymentMethod
{
    CreditCard,
    PayPal,
    Crypto,
    BankTransfer,
    PlatformWallet
}

/// <summary>
/// Payment result.
/// </summary>
public class MugenPrizePoolServicePaymentResult
{
    public string TransactionId { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public MugenPrizePoolServicePaymentStatus Status { get; set; } = default!;
    public DateTime ProcessedAt { get; set; } = default!;
}

/// <summary>
/// Payment status enumeration.
/// </summary>
public enum MugenPrizePoolServicePaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

/// <summary>
/// Sponsorship contribution request.
/// </summary>
public class MugenPrizePoolServiceSponsorshipContributionRequest
{
    public string PrizePoolId { get; set; } = default!;
    public string SponsorId { get; set; } = default!;
    public string SponsorName { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public MugenPrizePoolServicePaymentMethod MugenPrizePoolServicePaymentMethod { get; set; } = default!;
    public string? LogoUrl { get; set; } = default!;
    public string? WebsiteUrl { get; set; } = default!;
    public MugenPrizePoolServiceSponsorshipVisibility VisibilityLevel { get; set; } = default!;
}

/// <summary>
/// Sponsorship visibility enumeration.
/// </summary>
public enum MugenPrizePoolServiceSponsorshipVisibility
{
    Banner,
    Logo,
    Named,
    Title
}

/// <summary>
/// Sponsor contribution.
/// </summary>
public class MugenPrizePoolServiceSponsorContribution
{
    public string SponsorId { get; set; } = default!;
    public string SponsorName { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public string PaymentId { get; set; } = default!;
    public DateTime ContributionDate { get; set; } = default!;
    public string? LogoUrl { get; set; } = default!;
    public string? WebsiteUrl { get; set; } = default!;
    public MugenPrizePoolServiceSponsorshipVisibility VisibilityLevel { get; set; } = default!;
    public DateTime AgreedAt { get; set; }
}

/// <summary>
/// Tournament entry.
/// </summary>
public class MugenPrizePoolServiceTournamentEntry
{
    public string EntryId { get; set; } = default!;
    public string PrizePoolId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public decimal EntryFee { get; set; } = default!;
    public DateTime EntryTime { get; set; } = default!;
    public string PaymentId { get; set; } = default!;
    public MugenPrizePoolServiceEntryStatus Status { get; set; } = default!;
}

/// <summary>
/// Entry status enumeration.
/// </summary>
public enum MugenPrizePoolServiceEntryStatus
{
    Pending,
    Confirmed,
    Refunded,
    Cancelled
}

/// <summary>
/// Prize distribution.
/// </summary>
public class MugenPrizePoolServicePrizeDistribution
{
    public string PrizePoolId { get; set; } = default!;
    public decimal TotalPool { get; set; } = default!;
    public decimal HouseCut { get; set; } = default!;
    public IReadOnlyList<MugenPrizePoolServicePrizeTier> Prizes { get; set; } = default!;
    public DateTime CalculatedAt { get; set; } = default!;
    public decimal TotalPayout => Prizes.Sum(p => p.Amount);

    public decimal GetPrizeForPlacement(int placement)
    {
        return Prizes.FirstOrDefault(p => p.Placement == placement)?.Amount ?? 0;
    }
}

/// <summary>
/// Prize tier.
/// </summary>
public class MugenPrizePoolServicePrizeTier
{
    public int Placement { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public decimal Percentage { get; set; } = default!;
}

/// <summary>
/// Tournament result.
/// </summary>
public class MugenPrizePoolServiceTournamentResult
{
    public string PlayerId { get; set; } = default!;
    public int Placement { get; set; } = default!;
    public string? TeamId { get; set; } = default!;
}

/// <summary>
/// Prize pool transaction.
/// </summary>
public class MugenPrizePoolServicePrizePoolTransaction
{
    public string TransactionId { get; set; } = default!;
    public MugenPrizePoolServiceTransactionType Type { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public bool Processed { get; set; } = default!;
    public string? PlayerId { get; set; } = default!;
    public string? SponsorId { get; set; } = default!;
}

/// <summary>
/// Transaction type enumeration.
/// </summary>
public enum MugenPrizePoolServiceTransactionType
{
    EntryFee,
    Sponsorship,
    HouseContribution,
    PrizePayout,
    Refund
}

/// <summary>
/// Payout result.
/// </summary>
public class MugenPrizePoolServicePayoutResult
{
    public string RecipientId { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public MugenPrizePoolServicePayoutStatus Status { get; set; } = default!;
    public DateTime ProcessedAt { get; set; } = default!;
    public string TransactionId { get; set; } = default!;
}

/// <summary>
/// Payout status enumeration.
/// </summary>
public enum MugenPrizePoolServicePayoutStatus
{
    Pending,
    Completed,
    Failed
}

/// <summary>
/// Prize pool analytics.
/// </summary>
public class MugenPrizePoolServicePrizePoolAnalytics
{
    public string PrizePoolId { get; set; } = default!;
    public decimal TotalPool { get; set; } = default!;
    public decimal EntryFeesCollected { get; set; } = default!;
    public decimal SponsorshipRevenue { get; set; } = default!;
    public decimal HouseContribution { get; set; } = default!;
    public int ParticipantsCount { get; set; } = default!;
    public double CapacityUtilization { get; set; } = default!;
    public decimal AverageEntryFee { get; set; } = default!;
    public decimal ProjectedPayout { get; set; } = default!;
    public int TransactionCount { get; set; } = default!;
    public DateTime LastActivity { get; set; } = default!;
}

/// <summary>
/// Payment transaction.
/// </summary>
public class MugenPrizePoolServicePaymentTransaction
{
    public string TransactionId { get; set; } = default!;
    public string PayerId { get; set; } = default!;
    public decimal Amount { get; set; } = default!;
    public MugenPrizePoolServicePaymentMethod Method { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public MugenPrizePoolServicePaymentStatus Status { get; set; } = default!;
}
