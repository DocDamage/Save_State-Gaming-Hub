namespace SaveState.Application.Mugen.Models.SocialFeatures;

/// <summary>
/// Types of social features available.
/// </summary>
public enum SocialFeatureType
{
    Profile,
    Friends,
    Messaging,
    Reputation,
    Reporting,
    Presence
}

/// <summary>
/// Content types for sharing.
/// </summary>
public enum ContentType
{
    Replay,
    Screenshot,
    Character,
    Stage,
    Configuration,
    TournamentBracket
}

/// <summary>
/// Event types for community events.
/// </summary>
public enum EventType
{
    Tournament,
    CasualMeetup,
    Workshop,
    Competition,
    Celebration
}

/// <summary>
/// Clan ranks.
/// </summary>
public enum ClanRank
{
    Member,
    Officer,
    Leader,
    Founder
}

/// <summary>
/// Clan member status.
/// </summary>
public enum ClanMemberStatus
{
    Active,
    Inactive,
    Suspended,
    Left
}

/// <summary>
/// Streaming platforms.
/// </summary>
public enum StreamingPlatform
{
    Twitch,
    YouTube,
    Facebook,
    Discord
}

/// <summary>
/// Privacy levels for shared content.
/// </summary>
public enum ContentPrivacy
{
    Public,
    FriendsOnly,
    ClanOnly,
    Private
}
