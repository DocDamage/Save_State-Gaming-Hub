namespace SaveState.Presentation.Models.Security;

/// <summary>
/// Represents a user account in the system.
/// </summary>
public class UserAccount
{
    /// <summary>Unique identifier for the user.</summary>
    public Guid Id { get; set; }

    /// <summary>Username for login.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Email address of the user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Role assigned to the user (Admin, User, Guest).</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>List of roles assigned to the user (for multi-role support).</summary>
    public List<Role> Roles { get; set; } = new();

    /// <summary>Timestamp when the account was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Timestamp of the last login, if any.</summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>Whether the account is currently active.</summary>
    public bool IsActive { get; set; }

    /// <summary>URL to the user's profile image.</summary>
    public string? ProfileImageUrl { get; set; }

    /// <summary>Formatted display of the user's roles.</summary>
    public string RoleDisplay => Roles.Count > 0 ? string.Join(", ", Roles.Select(r => r.Name)) : Role;

    /// <summary>Status indicator text.</summary>
    public string StatusText => IsActive ? "Active" : "Inactive";

    /// <summary>Status color brush key.</summary>
    public string StatusBrushKey => IsActive ? "SuccessBrush" : "DangerBrush";
}

/// <summary>
/// Represents a role in the system.
/// </summary>
public class Role
{
    /// <summary>Unique identifier for the role.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Name of the role.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the role.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>List of permissions associated with this role.</summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>Whether this is a system role that cannot be deleted.</summary>
    public bool IsSystem { get; set; }

    /// <summary>Number of users with this role.</summary>
    public int UserCount { get; set; }
}

/// <summary>
/// Represents an API key for external application access.
/// </summary>
public class ApiKey
{
    /// <summary>Unique identifier for the API key.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name for the API key.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the API key's purpose.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Masked representation of the API key (for display).</summary>
    public string MaskedKey { get; set; } = string.Empty;

    /// <summary>Timestamp when the key was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Timestamp when the key expires, if any.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Timestamp of the last usage, if any.</summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>List of permissions/scopes granted to this key.</summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>List of scopes granted to this key.</summary>
    public List<string> Scopes { get; set; } = new();

    /// <summary>Whether the key is currently active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Username of the user who created this key.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Formatted permissions display.</summary>
    public string PermissionsDisplay => Permissions.Count > 0 ? string.Join(", ", Permissions) : string.Join(", ", Scopes);

    /// <summary>Expiration status text.</summary>
    public string ExpirationStatus
    {
        get
        {
            if (!IsActive) return "Revoked";
            if (ExpiresAt is null) return "Never";
            if (ExpiresAt < DateTime.UtcNow) return "Expired";
            var days = (ExpiresAt.Value - DateTime.UtcNow).Days;
            return days <= 7 ? $"{days} days" : $"{days} days";
        }
    }

    /// <summary>Whether the key is expired.</summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>Days until expiration (negative if expired).</summary>
    public int DaysUntilExpiration => ExpiresAt.HasValue ? (ExpiresAt.Value - DateTime.UtcNow).Days : int.MaxValue;
}

/// <summary>
/// Represents a user session for session management.
/// </summary>
public class UserSession
{
    /// <summary>Unique session identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>User ID associated with this session.</summary>
    public Guid UserId { get; set; }

    /// <summary>IP address of the session.</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>User agent string of the session.</summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>When the session was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the session was last active.</summary>
    public DateTime LastActiveAt { get; set; }

    /// <summary>When the session expires.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Whether this is the current session.</summary>
    public bool IsCurrentSession { get; set; }

    /// <summary>Device/OS information.</summary>
    public string DeviceInfo { get; set; } = string.Empty;

    /// <summary>Location information.</summary>
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// API key usage statistics.
/// </summary>
public class ApiKeyUsageStats
{
    /// <summary>Total API calls made today.</summary>
    public int TotalCallsToday { get; set; }

    /// <summary>Most active key name.</summary>
    public string MostActiveKeyName { get; set; } = string.Empty;

    /// <summary>Number of calls by the most active key.</summary>
    public int MostActiveKeyCalls { get; set; }

    /// <summary>Calls in the last hour.</summary>
    public int CallsLastHour { get; set; }

    /// <summary>Average response time in milliseconds.</summary>
    public double AverageResponseTimeMs { get; set; }
}
