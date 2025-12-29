using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.UserManagement.DTOs;

public class UserProfileDto
{
    public UserId Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
}
