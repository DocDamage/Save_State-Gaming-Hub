namespace SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// Portal user account information.
/// </summary>
public class PortalUser
{
    public string UserId { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
}

/// <summary>
/// User profile data.
/// </summary>
public class WebPortalServiceUserProfile
{
    public string UserId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Bio { get; set; } = default!;
    public string AvatarUrl { get; set; } = default!;
    public string? Location { get; set; } = default!;
    public string? Website { get; set; } = default!;
    public DateTime JoinDate { get; set; } = default!;
    public DateTime LastActivity { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
    public IReadOnlyList<string> PreferredCharacters { get; set; } = default!;
    public IReadOnlyDictionary<string, string> StreamingLinks { get; set; } = default!;
    public WebPortalServiceUserStats Stats { get; set; } = default!;
    public IReadOnlyList<WebPortalServiceUserAchievement> Achievements { get; set; } = default!;
    public IReadOnlyDictionary<string, string> SocialLinks { get; set; } = default!;
}

/// <summary>
/// User session information.
/// </summary>
public class UserSession
{
    public string SessionId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string IpAddress { get; set; } = default!;
    public string UserAgent { get; set; } = default!;
    public bool IsValid { get; set; }
}

/// <summary>
/// Profile update request.
/// </summary>
public class WebPortalServiceProfileUpdateRequest
{
    public string? DisplayName { get; set; } = default!;
    public string? Bio { get; set; } = default!;
    public string? AvatarUrl { get; set; } = default!;
    public string? Location { get; set; } = default!;
    public string? Website { get; set; } = default!;
    public IReadOnlyList<string>? PreferredCharacters { get; set; } = default!;
    public IReadOnlyDictionary<string, string>? StreamingLinks { get; set; } = default!;
}

/// <summary>
/// User stats.
/// </summary>
public class WebPortalServiceUserStats
{
    public int TotalMatches { get; set; } = default!;
    public int Wins { get; set; } = default!;
    public int Losses { get; set; } = default!;
    public double WinRate { get; set; } = default!;
    public int CurrentStreak { get; set; } = default!;
    public int BestStreak { get; set; } = default!;
    public string? FavoriteCharacter { get; set; } = default!;
    public string Rank { get; set; } = default!;
    public int Rating { get; set; } = default!;
}

/// <summary>
/// User achievement.
/// </summary>
public class WebPortalServiceUserAchievement
{
    public string AchievementId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime UnlockedAt { get; set; } = default!;
    public string IconUrl { get; set; } = default!;
}

/// <summary>
/// Top contributor data.
/// </summary>
public class WebPortalServiceTopContributor
{
    public string UserId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public int ContributionScore { get; set; } = default!;
    public WebPortalServiceContributionType Category { get; set; } = default!;
}
