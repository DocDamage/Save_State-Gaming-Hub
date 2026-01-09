using SaveState.Core.Common.Base;

namespace SaveState.Core.UserManagement.Entities;

/// <summary>
/// Represents a user in the system with authentication and authorization capabilities.
/// </summary>
public class User : EntityBase
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public bool IsEmailVerified { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<ApiKey> _apiKeys = new();
    public IReadOnlyCollection<ApiKey> ApiKeys => _apiKeys.AsReadOnly();

    private User() { }

    public static User Create(string username, string email, string passwordHash, string passwordSalt)
    {
        return new User
        {
            Username = Guard.Against.NullOrWhiteSpace(username, nameof(username)),
            Email = Guard.Against.NullOrWhiteSpace(email, nameof(email)),
            PasswordHash = Guard.Against.NullOrWhiteSpace(passwordHash, nameof(passwordHash)),
            PasswordSalt = Guard.Against.NullOrWhiteSpace(passwordSalt, nameof(passwordSalt)),
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
            IsEmailVerified = false
        };
    }

    public void UpdateUsername(string username)
    {
        Username = Guard.Against.NullOrWhiteSpace(username, nameof(username));
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void ChangePassword(string newPasswordHash, string newPasswordSalt)
    {
        PasswordHash = Guard.Against.NullOrWhiteSpace(newPasswordHash, nameof(newPasswordHash));
        PasswordSalt = Guard.Against.NullOrWhiteSpace(newPasswordSalt, nameof(newPasswordSalt));
    }

    public void AddRole(Role role)
    {
        Guard.Against.Null(role, nameof(role));

        if (!_userRoles.Any(ur => ur.RoleId == role.Id))
        {
            _userRoles.Add(UserRole.Create(this, role));
        }
    }

    public void RemoveRole(Role role)
    {
        Guard.Against.Null(role, nameof(role));

        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == role.Id);
        if (userRole != null)
        {
            _userRoles.Remove(userRole);
        }
    }

    public bool HasRole(string roleName)
    {
        return _userRoles.Any(ur => ur.Role.Name == roleName);
    }

    public bool HasAnyRole(IEnumerable<string> roleNames)
    {
        return _userRoles.Any(ur => roleNames.Contains(ur.Role.Name));
    }

    public ApiKey CreateApiKey(string name, string description, DateTimeOffset? expiresAt = null)
    {
        var apiKey = ApiKey.Create(this, name, description, expiresAt);
        _apiKeys.Add(apiKey);
        return apiKey;
    }

    public void RevokeApiKey(Guid apiKeyId)
    {
        var apiKey = _apiKeys.FirstOrDefault(k => k.Id == apiKeyId);
        if (apiKey != null)
        {
            apiKey.Revoke();
        }
    }
}
