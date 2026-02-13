global using SocialReport = SaveState.Application.Mugen.Models.NetworkFeatures.PlayerReport;
global using SocialReportReason = SaveState.Application.Mugen.Models.NetworkFeatures.ReportReason;
global using SocialReportStatus = SaveState.Application.Mugen.Models.NetworkFeatures.ReportStatus;
global using SocialReputationTier = SaveState.Application.Mugen.Models.NetworkFeatures.ReputationTier;
global using SocialFriendshipStatus = SaveState.Application.Mugen.Models.NetworkFeatures.FriendshipStatus;
global using SocialPlayerOnlineStatus = SaveState.Application.Mugen.Models.NetworkFeatures.PlayerOnlineStatus;
global using SocialChatChannel = SaveState.Application.Mugen.Models.NetworkFeatures.ChatChannel;
global using SocialPlayerProfile = SaveState.Application.Mugen.Models.NetworkFeatures.PlayerProfile;
global using SocialFriendInfo = SaveState.Application.Mugen.Models.NetworkFeatures.FriendInfo;
global using SocialChatMessage = SaveState.Application.Mugen.Models.NetworkFeatures.ChatMessage;
global using SocialAchievement = SaveState.Application.Mugen.Models.NetworkFeatures.Achievement;
global using SocialPlayerStats = SaveState.Application.Mugen.Models.NetworkFeatures.PlayerStats;
global using SocialCharacterSpecificStats = SaveState.Application.Mugen.Models.NetworkFeatures.CharacterSpecificStats;
global using SocialReputation = SaveState.Application.Mugen.Models.NetworkFeatures.Reputation;

namespace SaveState.Application.Mugen.Models.SocialFeatures;

/// <summary>
/// Type aliases for social features to reduce verbosity.
/// </summary>
public static class SocialTypeAliases
{
    /// <summary>
    /// Short alias for friend request tuple.
    /// </summary>
    public const string FriendRequestId = nameof(FriendRequestId);

    /// <summary>
    /// Short alias for friendship ID.
    /// </summary>
    public const string FriendshipId = nameof(FriendshipId);
}
