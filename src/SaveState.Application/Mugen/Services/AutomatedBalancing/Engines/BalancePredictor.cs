using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.AutomatedBalancing.Engines;

/// <summary>
/// Predicts balance outcomes.
/// </summary>
public class BalancePredictor
{
    private readonly ILogger<BalancePredictor> _logger;

    public BalancePredictor(ILogger<BalancePredictor> logger)
    {
        _logger = logger;
    }

    public async Task<BalancePrediction> PredictImpactAsync(BalanceAdjustment adjustment, CancellationToken ct = default)
    {
        _logger.LogInformation("Predicting impact for adjustment {AdjustmentId}", adjustment.AdjustmentId);

        return new BalancePrediction
        {
            AdjustmentId = adjustment.AdjustmentId,
            PredictedWinRateChange = adjustment.Type == AdjustmentType.Nerf ? -0.05 : 0.05,
            PredictedPickRateChange = adjustment.Type == AdjustmentType.Nerf ? -0.03 : 0.03,
            Confidence = 0.75,
            PotentialRisks = new[] { "Meta shift", "Secondary character impact" },
            PredictedAt = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<BalancePrediction>> BatchPredictAsync(IReadOnlyList<BalanceAdjustment> adjustments, CancellationToken ct = default)
    {
        var predictions = new List<BalancePrediction>();
        foreach (var adjustment in adjustments)
        {
            predictions.Add(await PredictImpactAsync(adjustment, ct));
        }
        return predictions;
    }
}

/// <summary>
/// Balance impact prediction.
/// </summary>
public class BalancePrediction
{
    public string AdjustmentId { get; set; } = default!;
    public double PredictedWinRateChange { get; set; }
    public double PredictedPickRateChange { get; set; }
    public double Confidence { get; set; }
    public IReadOnlyList<string> PotentialRisks { get; set; } = Array.Empty<string>();
    public DateTime PredictedAt { get; set; }
}