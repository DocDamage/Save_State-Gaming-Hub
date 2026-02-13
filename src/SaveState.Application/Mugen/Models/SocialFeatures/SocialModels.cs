namespace SaveState.Application.Mugen.Models.SocialFeatures;

/// <summary>
/// Player profile update data.
/// </summary>
public class PlayerProfileUpdate
{
    public string? DisplayName { get; set; }
    public IReadOnlyList<string>? FavoriteCharacters { get; set; }
    public string? StatusMessage { get; set; }
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// Friend request data.
/// </summary>
public class FriendRequest
{
    public string RequestId { get; set; } = default!;
    public string FromPlayerId { get; set; } = default!;
    public string FromPlayerName { get; set; } = default!;
    public DateTime RequestedAt { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Online player information.
/// </summary>
public class OnlinePlayer
{
    public string PlayerId { get; set; } = default!;
    public string PlayerName { get; set; } = default!;
    public string? CurrentActivity { get; set; }
    public DateTime LastSeen { get; set; }
}

/// <summary>
/// Friendship relationship.
/// </summary>
public class Friendship
{
    public string Id { get; set; } = string.Empty;
    public string Player1Id { get; set; } = string.Empty;
    public string Player2Id { get; set; } = string.Empty;
    public SaveState.Application.Mugen.Models.NetworkFeatures.FriendshipStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
    public DateTime? BlockedAt { get; set; }
    public string? BlockedBy { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Player reputation data.
/// </summary>
public class PlayerReputation
{
    public string PlayerId { get; set; } = default!;
    public int Score { get; set; }
    public SaveState.Application.Mugen.Models.NetworkFeatures.ReputationTier Tier { get; set; }
    public int ReportsReceived { get; set; }
    public int PositiveInteractions { get; set; }
    public DateTime LastActivity { get; set; }
}

/// <summary>
/// Clan information.
/// </summary>
public class ClanInfo
{
    public string ClanId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Tag { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? LogoUrl { get; set; }
    public string LeaderId { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public int MaxMembers { get; set; }
}

/// <summary>
/// Clan member information.
/// </summary>
public class ClanMember
{
    public string PlayerId { get; set; } = default!;
    public string PlayerName { get; set; } = default!;
    public ClanRank Rank { get; set; }
    public ClanMemberStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
}

/// <summary>
/// Clan activity record.
/// </summary>
public class ClanActivity
{
    public string ActivityId { get; set; } = default!;
    public string ClanId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public string ActivityType { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Tournament social information.
/// </summary>
public class TournamentSocialInfo
{
    public string TournamentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string OrganizerId { get; set; } = default!;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int ParticipantCount { get; set; }
    public int MaxParticipants { get; set; }
    public bool IsPublic { get; set; }
}

/// <summary>
/// Bracket sharing information.
/// </summary>
public class BracketSharing
{
    public string BracketId { get; set; } = default!;
    public string TournamentId { get; set; } = default!;
    public string? ShareCode { get; set; }
    public string? EmbedUrl { get; set; }
    public ContentPrivacy Privacy { get; set; }
    public DateTime SharedAt { get; set; }
}

/// <summary>
/// Stream information.
/// </summary>
public class StreamInfo
{
    public string StreamId { get; set; } = default!;
    public string PlayerId { get; set; } = default!;
    public string StreamTitle { get; set; } = default!;
    public StreamingPlatform Platform { get; set; }
    public string PlatformStreamId { get; set; } = default!;
    public bool IsLive { get; set; }
    public int? ViewerCount { get; set; }
    public DateTime StartedAt { get; set; }
}

/// <summary>
/// Stream settings.
/// </summary>
public class StreamSettings
{
    public bool AutoShareMatches { get; set; }
    public bool ShowReplayOverlay { get; set; }
    public bool EnableChatIntegration { get; set; }
    public IReadOnlyList<StreamingPlatform> ConnectedPlatforms { get; set; } = Array.Empty<StreamingPlatform>();
}

/// <summary>
/// Shared content information.
/// </summary>
public class SharedContent
{
    public string ContentId { get; set; } = default!;
    public string OwnerId { get; set; } = default!;
    public ContentType Type { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string ContentUrl { get; set; } = default!;
    public ContentPrivacy Privacy { get; set; }
    public DateTime CreatedAt { get; set; }
    public ContentMetrics Metrics { get; set; } = default!;
}

/// <summary>
/// Content metrics.
/// </summary>
public class ContentMetrics
{
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int ShareCount { get; set; }
    public int DownloadCount { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Community event information.
/// </summary>
public class CommunityEvent
{
    public string EventId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public EventType Type { get; set; }
    public string OrganizerId { get; set; } = default!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int MaxParticipants { get; set; }
    public EventStatus Status { get; set; }
}

/// <summary>
/// Event status.
/// </summary>
public enum EventStatus
{
    Draft,
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>
/// Event participant information.
/// </summary>
public class EventParticipant
{
    public string PlayerId { get; set; } = default!;
    public string PlayerName { get; set; } = default!;
    public DateTime RegisteredAt { get; set; }
    public bool IsAttending { get; set; }
}
