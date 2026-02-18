using SaveState.Application.Mugen.Models.NetworkFeatures;
using SaveState.Application.Mugen.Models.SocialFeatures;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.SocialFeatures;

/// <summary>
/// Engine for managing player reputation and reports.
/// </summary>
public sealed class ReputationEngine
{
    private readonly ILogger<ReputationEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public ReputationEngine(ILogger<ReputationEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates a default reputation for a player.
    /// </summary>
    public PlayerReputation CreateDefaultReputation(string playerId)
    {
        return new PlayerReputation
        {
            PlayerId = playerId,
            Score = 1000,
            Tier = ReputationTier.Neutral,
            ReportsReceived = 0,
            PositiveInteractions = 0,
            LastActivity = _timeProvider.UtcNow
        };
    }

    /// <summary>
    /// Creates a player report.
    /// </summary>
    public PlayerReport CreateReport(
        string reporterId,
        string reportedPlayerId,
        ReportReason reason,
        string description)
    {
        var report = new PlayerReport
        {
            ReportId = Guid.NewGuid().ToString(),
            ReporterId = reporterId,
            ReportedPlayerId = reportedPlayerId,
            Reason = reason,
            Description = description,
            SubmittedAt = _timeProvider.UtcNow,
            Status = ReportStatus.Pending
        };

        _logger.LogInformation("Created report {ReportId} against {ReportedPlayer}",
            report.ReportId, reportedPlayerId);
        return report;
    }

    /// <summary>
    /// Updates reputation based on a report reason.
    /// </summary>
    public void UpdateReputationForReport(PlayerReputation reputation, ReportReason reason)
    {
        var scoreChange = reason switch
        {
            ReportReason.Cheating => -100,
            ReportReason.Harassment => -75,
            ReportReason.InappropriateBehavior => -50,
            ReportReason.Spam => -25,
            ReportReason.Griefing => -50,
            _ => -25
        };

        reputation.Score = Math.Max(0, reputation.Score + scoreChange);
        reputation.ReportsReceived++;
        reputation.Tier = CalculateReputationTier(reputation.Score);
        reputation.LastActivity = _timeProvider.UtcNow;

        _logger.LogDebug("Updated reputation for {PlayerId}: score {Score}, tier {Tier}",
            reputation.PlayerId, reputation.Score, reputation.Tier);
    }

    /// <summary>
    /// Calculates reputation tier based on score.
    /// </summary>
    public ReputationTier CalculateReputationTier(int score)
    {
        return score switch
        {
            >= 1500 => ReputationTier.Excellent,
            >= 1200 => ReputationTier.Good,
            >= 800 => ReputationTier.Neutral,
            >= 500 => ReputationTier.Poor,
            _ => ReputationTier.Toxic
        };
    }

    /// <summary>
    /// Adds a positive interaction to a player's reputation.
    /// </summary>
    public void AddPositiveInteraction(PlayerReputation reputation)
    {
        reputation.PositiveInteractions++;
        reputation.Score = Math.Min(2000, reputation.Score + 5);
        reputation.Tier = CalculateReputationTier(reputation.Score);
        reputation.LastActivity = _timeProvider.UtcNow;

        _logger.LogDebug("Added positive interaction for {PlayerId}", reputation.PlayerId);
    }

    /// <summary>
    /// Gets pending reports from a list.
    /// </summary>
    public IEnumerable<PlayerReport> GetPendingReports(IEnumerable<PlayerReport> reports)
    {
        return reports.Where(r => r.Status == ReportStatus.Pending);
    }

    /// <summary>
    /// Gets reports for a specific player.
    /// </summary>
    public IEnumerable<PlayerReport> GetReportsForPlayer(IEnumerable<PlayerReport> reports, string playerId)
    {
        return reports.Where(r => r.ReportedPlayerId == playerId);
    }
}
