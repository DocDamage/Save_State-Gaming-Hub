namespace SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// User roles within the web portal.
/// </summary>
public enum UserRole
{
    Guest,
    Member,
    Moderator,
    Administrator,
    Developer
}

/// <summary>
/// Content publication status.
/// </summary>
public enum ContentStatus
{
    Draft,
    PendingReview,
    Published,
    Archived,
    Rejected
}

/// <summary>
/// API endpoint type classification.
/// </summary>
public enum ApiEndpointType
{
    Public,
    Authenticated,
    Admin,
    Internal
}

/// <summary>
/// Forum category types.
/// </summary>
public enum WebPortalServiceForumCategory
{
    General,
    CharacterCreation,
    StageCreation,
    Tutorials,
    Tournaments,
    OffTopic
}

/// <summary>
/// Forum post types.
/// </summary>
public enum WebPortalServicePostType
{
    ThreadStarter,
    Reply,
    Announcement
}

/// <summary>
/// Forum sorting options.
/// </summary>
public enum WebPortalServiceForumSort
{
    LastActivity,
    CreatedDate,
    ViewCount,
    ReplyCount
}

/// <summary>
/// Leaderboard types.
/// </summary>
public enum WebPortalServiceLeaderboardType
{
    Global,
    Regional,
    CharacterSpecific,
    Tournament
}

/// <summary>
/// Time frame options for leaderboards.
/// </summary>
public enum WebPortalServiceTimeFrame
{
    Daily,
    Weekly,
    Monthly,
    AllTime
}

/// <summary>
/// Content submission status.
/// </summary>
public enum WebPortalServiceSubmissionStatus
{
    PendingReview,
    Approved,
    Rejected,
    RequiresRevision
}

/// <summary>
/// Content types for submissions.
/// </summary>
public enum WebPortalServiceContentType
{
    Character,
    Stage,
    Music,
    Screenpack,
    Mod,
    Misc
}

/// <summary>
/// Content sorting options.
/// </summary>
public enum WebPortalServiceContentSort
{
    DownloadCount,
    Rating,
    Recent,
    Alphabetical
}

/// <summary>
/// Social activity types.
/// </summary>
public enum WebPortalServiceSocialActivityType
{
    MatchResult,
    Achievement,
    ContentShared,
    StatusUpdate,
    FriendActivity
}

/// <summary>
/// Contribution types for community metrics.
/// </summary>
public enum WebPortalServiceContributionType
{
    ForumActivity,
    ContentCreation,
    TournamentParticipation,
    CommunityHelp
}
