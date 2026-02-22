using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Threat detection engine for identifying security threats.
/// </summary>
public class EnterpriseSecurityThreatDetectionEngine
{
    private readonly ILogger<EnterpriseSecurityThreatDetectionEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly List<EnterpriseSecurityServiceSecurityEvent> _detectedThreats = new();

    public EnterpriseSecurityThreatDetectionEngine(
        ILogger<EnterpriseSecurityThreatDetectionEngine> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public void AnalyzeEvent(EnterpriseSecurityServiceSecurityEvent securityEvent)
    {
        _logger.LogDebug("Analyzing security event {EventType} from user {UserId}",
            securityEvent.EventType, securityEvent.UserId);

        // Simple threat detection logic
        var isThreat = securityEvent.EventType switch
        {
            EnterpriseSecurityServiceSecurityEventType.AccessDenied => true,
            EnterpriseSecurityServiceSecurityEventType.IncidentReported => true,
            _ => false
        };

        if (isThreat)
        {
            _detectedThreats.Add(securityEvent);
            _logger.LogWarning("Potential threat detected: {EventType}", securityEvent.EventType);
        }
    }

    public EnterpriseSecurityServiceThreatMetrics GetMetrics(TimeSpan period)
    {
        var recentThreats = _detectedThreats
            .Where(t => _timeProvider.UtcNow - t.Timestamp <= period)
            .ToList();

        return new EnterpriseSecurityServiceThreatMetrics
        {
            TotalThreats = recentThreats.Count,
            BlockedThreats = recentThreats.Count(t => t.EventType == EnterpriseSecurityServiceSecurityEventType.AccessDenied),
            InvestigatingThreats = recentThreats.Count(t => t.EventType == EnterpriseSecurityServiceSecurityEventType.IncidentReported),
            ThreatsByType = recentThreats
                .GroupBy(t => t.EventType.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }
}
