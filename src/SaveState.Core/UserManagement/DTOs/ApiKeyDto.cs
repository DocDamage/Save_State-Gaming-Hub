using SaveState.Core.Common;

namespace SaveState.Core.UserManagement.DTOs;

/// <summary>
/// Data transfer object for API key information.
/// </summary>
public record ApiKeyDto(
    Guid Id,
    string Name,
    string Description,
    string KeyPrefix,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? LastUsedAt,
    bool IsActive,
    bool IsExpired);