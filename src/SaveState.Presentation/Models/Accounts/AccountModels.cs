namespace SaveState.Presentation.Models.Accounts;

/// <summary>
/// Represents the connection status of an external account.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>Account is connected and ready.</summary>
    Connected,

    /// <summary>Account is disconnected.</summary>
    Disconnected,

    /// <summary>Connection is in progress.</summary>
    Connecting,

    /// <summary>Connection encountered an error.</summary>
    Error,

    /// <summary>Connection is not available (deprecated, unavailable).</summary>
    NotAvailable
}

/// <summary>
/// Represents the status of a connected account for a specific platform.
/// </summary>
public class AccountConnectionStatus
{
    /// <summary>Name of the platform.</summary>
    public string PlatformName { get; set; } = string.Empty;

    /// <summary>Current connection status.</summary>
    public ConnectionStatus Status { get; set; }

    /// <summary>Username on the platform (if connected).</summary>
    public string? Username { get; set; }

    /// <summary>When the account was first connected.</summary>
    public DateTime? ConnectedSince { get; set; }

    /// <summary>When the account was last synchronized.</summary>
    public DateTime? LastSync { get; set; }

    /// <summary>Error message if connection failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Whether the account can be synchronized.</summary>
    public bool CanSync { get; set; }

    /// <summary>URL to the user's avatar.</summary>
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Represents the progress of an account linking operation.
/// </summary>
public class AccountLinkingProgress
{
    /// <summary>Current stage of the linking process.</summary>
    public string Stage { get; set; } = string.Empty;

    /// <summary>Progress percentage (0-100).</summary>
    public int ProgressPercent { get; set; }

    /// <summary>Description of the current action.</summary>
    public string? CurrentAction { get; set; }

    /// <summary>Whether the linking is complete.</summary>
    public bool IsComplete { get; set; }
}
