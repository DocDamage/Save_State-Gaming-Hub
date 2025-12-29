using SaveState.Core.Common.Base;

namespace SaveState.Core.GameLibrary.Entities;

public class User : EntityBase
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsActive { get; private set; }

    protected User() { } // EF Core

    public User(string username, string email)
    {
        Username = Guard.Against.NullOrWhiteSpace(username, nameof(username));
        Email = Guard.Against.NullOrWhiteSpace(email, nameof(email));
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDisplayName(string displayName)
    {
        DisplayName = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
    }

    public void UpdateEmail(string email)
    {
        Email = Guard.Against.NullOrWhiteSpace(email, nameof(email));
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
