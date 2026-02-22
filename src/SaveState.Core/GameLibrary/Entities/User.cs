using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

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

    public User(string username, string email, ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        Username = Guard.Against.NullOrWhiteSpace(username, nameof(username));
        Email = Guard.Against.NullOrWhiteSpace(email, nameof(email));
        IsActive = true;
        CreatedAt = timeProvider.UtcNow;
    }

    public User(string username, string email, DateTime createdAt)
    {
        Username = Guard.Against.NullOrWhiteSpace(username, nameof(username));
        Email = Guard.Against.NullOrWhiteSpace(email, nameof(email));
        IsActive = true;
        CreatedAt = createdAt;
    }

    [Obsolete("Use constructor with ITimeProvider or DateTime parameter")]
    public User(string username, string email)
    {
        Username = Guard.Against.NullOrWhiteSpace(username, nameof(username));
        Email = Guard.Against.NullOrWhiteSpace(email, nameof(email));
        IsActive = true;
        CreatedAt = SystemTimeProvider.Instance.UtcNow;
    }

    public void UpdateDisplayName(string displayName)
    {
        DisplayName = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
    }

    public void UpdateEmail(string email)
    {
        Email = Guard.Against.NullOrWhiteSpace(email, nameof(email));
    }

    public void RecordLogin(ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        LastLoginAt = timeProvider.UtcNow;
    }

    public void RecordLogin(DateTime timestamp)
    {
        LastLoginAt = timestamp;
    }

    [Obsolete("Use RecordLogin(ITimeProvider) or RecordLogin(DateTime) instead")]
    public void RecordLogin()
    {
        LastLoginAt = SystemTimeProvider.Instance.UtcNow;
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
