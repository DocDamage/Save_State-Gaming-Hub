using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Access control engine for evaluating permissions and making access decisions.
/// </summary>
public class EnterpriseSecurityAccessControlEngine
{
    private readonly ILogger<EnterpriseSecurityAccessControlEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, List<EnterpriseSecurityServicePermission>> _userPermissions = new();

    public EnterpriseSecurityAccessControlEngine(
        ILogger<EnterpriseSecurityAccessControlEngine> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public EnterpriseSecurityServiceAccessControlDecision EvaluateAccess(
        string resourceId,
        string userId,
        EnterpriseSecurityServicePermission requestedPermission)
    {
        _logger.LogDebug("Evaluating access for user {UserId} to resource {ResourceId} for permission {Permission}",
            userId, resourceId, requestedPermission);

        // Check if user has the requested permission
        if (_userPermissions.TryGetValue(userId, out var permissions))
        {
            if (permissions.Contains(requestedPermission) || permissions.Contains(EnterpriseSecurityServicePermission.Admin))
            {
                return new EnterpriseSecurityServiceAccessControlDecision
                {
                    Decision = EnterpriseSecurityServiceAccessDecision.Granted,
                    ResourceId = resourceId,
                    UserId = userId,
                    RequestedPermission = requestedPermission,
                    Reason = "Permission granted",
                    Timestamp = _timeProvider.UtcNow
                };
            }
        }

        return new EnterpriseSecurityServiceAccessControlDecision
        {
            Decision = EnterpriseSecurityServiceAccessDecision.Denied,
            ResourceId = resourceId,
            UserId = userId,
            RequestedPermission = requestedPermission,
            Reason = "Insufficient permissions",
            Timestamp = _timeProvider.UtcNow
        };
    }

    public void GrantPermission(string userId, EnterpriseSecurityServicePermission permission)
    {
        if (!_userPermissions.TryGetValue(userId, out var permissions))
        {
            permissions = new List<EnterpriseSecurityServicePermission>();
            _userPermissions[userId] = permissions;
        }

        if (!permissions.Contains(permission))
        {
            permissions.Add(permission);
            _logger.LogInformation("Granted {Permission} to user {UserId}", permission, userId);
        }
    }

    public void RevokePermission(string userId, EnterpriseSecurityServicePermission permission)
    {
        if (_userPermissions.TryGetValue(userId, out var permissions))
        {
            permissions.Remove(permission);
            _logger.LogInformation("Revoked {Permission} from user {UserId}", permission, userId);
        }
    }
}
