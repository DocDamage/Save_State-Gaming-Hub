// Type aliases for backward compatibility after refactoring
// These aliases allow existing code to continue using the old type names
// while the new code uses the cleaner, shorter names

// Using aliases must come first
using NetworkFeaturesServiceMatchmakingRequest = SaveState.Application.Mugen.Models.NetworkFeatures.MatchmakingRequest;
using NetworkFeaturesServiceMatchmakingPreferences = SaveState.Application.Mugen.Models.NetworkFeatures.MatchmakingPreferences;
using NetworkFeaturesServiceMatchmakingResult = SaveState.Application.Mugen.Models.NetworkFeatures.MatchmakingResult;
using NetworkFeaturesServiceLobbyCreationRequest = SaveState.Application.Mugen.Models.NetworkFeatures.LobbyCreationRequest;
using NetworkFeaturesServiceLobbySettings = SaveState.Application.Mugen.Models.NetworkFeatures.LobbySettings;
using NetworkFeaturesServiceLobbyInfo = SaveState.Application.Mugen.Models.NetworkFeatures.LobbyInfo;
using NetworkFeaturesServiceLobbyFilter = SaveState.Application.Mugen.Models.NetworkFeatures.LobbyFilter;
using NetworkFeaturesServiceLobbyPlayer = SaveState.Application.Mugen.Models.NetworkFeatures.LobbyPlayer;
using NetworkFeaturesServicePlayerProfile = SaveState.Application.Mugen.Models.NetworkFeatures.PlayerProfile;
using NetworkFeaturesServicePlayerStats = SaveState.Application.Mugen.Models.NetworkFeatures.PlayerStats;
using NetworkFeaturesServiceCharacterSpecificStats = SaveState.Application.Mugen.Models.NetworkFeatures.CharacterSpecificStats;
using NetworkFeaturesServiceReputation = SaveState.Application.Mugen.Models.NetworkFeatures.Reputation;
using NetworkFeaturesServiceLeaderboardEntry = SaveState.Application.Mugen.Models.NetworkFeatures.LeaderboardEntry;
using NetworkFeaturesServiceFriendInfo = SaveState.Application.Mugen.Models.NetworkFeatures.FriendInfo;
using NetworkFeaturesServiceAchievement = SaveState.Application.Mugen.Models.NetworkFeatures.Achievement;
using NetworkFeaturesServiceSpectatorSession = SaveState.Application.Mugen.Models.NetworkFeatures.SpectatorSession;
using NetworkFeaturesServiceSpectatorControls = SaveState.Application.Mugen.Models.NetworkFeatures.SpectatorControls;
using NetworkFeaturesServiceChatMessage = SaveState.Application.Mugen.Models.NetworkFeatures.ChatMessage;
using NetworkFeaturesServiceReplayData = SaveState.Application.Mugen.Models.NetworkFeatures.ReplayData;
using NetworkFeaturesServiceNetworkSession = SaveState.Application.Mugen.Models.NetworkFeatures.NetworkSession;
using NetworkFeaturesServicePlayerMatchmakingStats = SaveState.Application.Mugen.Models.NetworkFeatures.PlayerMatchmakingStats;
using NetworkFeaturesServiceMatchmakingSession = SaveState.Application.Mugen.Models.NetworkFeatures.MatchmakingSession;
using NetworkFeaturesServiceQueuedPlayer = SaveState.Application.Mugen.Models.NetworkFeatures.QueuedPlayer;
using NetworkFeaturesServiceCharacterMatchupData = SaveState.Application.Mugen.Models.NetworkFeatures.CharacterMatchupData;
using NetworkFeaturesServiceNetworkQualityMetrics = SaveState.Application.Mugen.Models.NetworkFeatures.NetworkQualityMetrics;
using NetworkFeaturesServiceRelayServerInfo = SaveState.Application.Mugen.Models.NetworkFeatures.RelayServerInfo;

// Enum aliases
using NetworkFeaturesServiceMatchmakingMode = SaveState.Application.Mugen.Models.NetworkFeatures.MatchmakingMode;
using NetworkFeaturesServiceLobbyStatus = SaveState.Application.Mugen.Models.NetworkFeatures.LobbyStatus;
using NetworkFeaturesServiceNetworkQuality = SaveState.Application.Mugen.Models.NetworkFeatures.NetworkQuality;
using NetworkFeaturesServiceLeaderboardType = SaveState.Application.Mugen.Models.NetworkFeatures.LeaderboardType;
using NetworkFeaturesServiceReportReason = SaveState.Application.Mugen.Models.NetworkFeatures.ReportReason;
using NetworkFeaturesServiceReputationTier = SaveState.Application.Mugen.Models.NetworkFeatures.ReputationTier;
using NetworkFeaturesServiceFriendshipAction = SaveState.Application.Mugen.Models.NetworkFeatures.FriendshipAction;
using NetworkFeaturesServiceFriendshipStatus = SaveState.Application.Mugen.Models.NetworkFeatures.FriendshipStatus;
using NetworkFeaturesServicePlayerOnlineStatus = SaveState.Application.Mugen.Models.NetworkFeatures.PlayerOnlineStatus;
using NetworkFeaturesServiceChatChannel = SaveState.Application.Mugen.Models.NetworkFeatures.ChatChannel;
using NetworkFeaturesServiceReportStatus = SaveState.Application.Mugen.Models.NetworkFeatures.ReportStatus;
using NetworkFeaturesServiceAchievementRarity = SaveState.Application.Mugen.Models.NetworkFeatures.AchievementRarity;
using NetworkFeaturesServiceNetworkSessionState = SaveState.Application.Mugen.Models.NetworkFeatures.NetworkSessionState;
using NetworkFeaturesServiceRelayRegion = SaveState.Application.Mugen.Models.NetworkFeatures.RelayRegion;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Player report data (backward compatibility alias).
/// </summary>
public class NetworkFeaturesServicePlayerReport
{
    public string ReportId { get; set; } = default!;
    public string ReporterId { get; set; } = default!;
    public string ReportedPlayerId { get; set; } = default!;
    public Core.Mugen.Services.ReportReason Reason { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime SubmittedAt { get; set; } = default!;
    public NetworkFeaturesServiceReportStatus Status { get; set; } = default!;
}

/// <summary>
/// Report status (backward compatibility alias).
/// </summary>
public enum NetworkFeaturesServiceReportStatus
{
    Pending,
    Investigating,
    Resolved,
    Dismissed
}
