using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Audit trail manager for logging and querying security events.
/// </summary>
public class EnterpriseSecurityAuditTrailManager
{
    private readonly ILogger<EnterpriseSecurityAuditTrailManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly List<EnterpriseSecurityServiceAuditLog> _auditLogs = new();

    public EnterpriseSecurityAuditTrailManager(
        ILogger<EnterpriseSecurityAuditTrailManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public void RecordEvent(EnterpriseSecurityServiceAuditEventType eventType, string userId, string resourceId, string action, bool success, string details)
    {
        var log = new EnterpriseSecurityServiceAuditLog
        {
            Timestamp = _timeProvider.UtcNow,
            EventType = eventType,
            UserId = userId,
            ResourceId = resourceId,
            Action = action,
            Success = success,
            Details = details
        };

        _auditLogs.Add(log);
        _logger.LogDebug("Recorded audit log: {EventType} by {UserId} on {ResourceId}", eventType, userId, resourceId);
    }

    public List<EnterpriseSecurityServiceAuditLog> QueryLogs(EnterpriseSecurityServiceAuditQuery query)
    {
        var results = _auditLogs.AsEnumerable();

        if (query.StartDate.HasValue)
            results = results.Where(l => l.Timestamp >= query.StartDate.Value);

        if (query.EndDate.HasValue)
            results = results.Where(l => l.Timestamp <= query.EndDate.Value);

        if (query.EventType.HasValue)
            results = results.Where(l => l.EventType == query.EventType.Value);

        if (!string.IsNullOrEmpty(query.UserId))
            results = results.Where(l => l.UserId == query.UserId);

        if (!string.IsNullOrEmpty(query.ResourceId))
            results = results.Where(l => l.ResourceId == query.ResourceId);

        results = results.OrderByDescending(l => l.Timestamp);

        if (query.MaxResults.HasValue)
            results = results.Take(query.MaxResults.Value);

        return results.ToList();
    }

    public void LogSecurityEvent(EnterpriseSecurityServiceSecurityEvent securityEvent)
    {
        RecordEvent(
            EnterpriseSecurityServiceAuditEventType.Security,
            securityEvent.UserId,
            securityEvent.ResourceId,
            securityEvent.EventType.ToString(),
            true,
            $"Security event: {securityEvent.EventType}"
        );
    }
}
