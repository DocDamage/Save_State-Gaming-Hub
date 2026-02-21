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

    /// <summary>Timestamp when the account was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Timestamp of the last login, if any.</summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>Whether the account is currently active.</summary>
    public bool IsActive { get; set; }
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

    /// <summary>Masked representation of the API key (for display).</summary>
    public string MaskedKey { get; set; } = string.Empty;

    /// <summary>Timestamp when the key was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Timestamp of the last usage, if any.</summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>List of permissions granted to this key.</summary>
    public List<string> Permissions { get; set; } = new();
}
