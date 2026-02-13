using SaveState.Application.Mugen.Models.NetworkFeatures;
using SaveState.Application.Mugen.Models.SocialFeatures;
using SaveState.Core.Common;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.SocialFeatures;

/// <summary>
/// Engine for managing player profiles and presence.
/// </summary>
public sealed class ProfileEngine
{
    private readonly ILogger<ProfileEngine> _logger;

    public ProfileEngine(ILogger<ProfileEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Applies profile updates to an existing profile.
    /// </summary>
    public PlayerProfile ApplyProfileUpdate(PlayerProfile profile, PlayerProfileUpdate update)
    {
        var result = profile;

        if (!string.IsNullOrEmpty(update.DisplayName))
        {
            result = result with { PlayerName = update.DisplayName };
        }

        if (update.FavoriteCharacters != null)
        {
            result = result with { FavoriteCharacters = update.FavoriteCharacters };
        }

        if (update.StatusMessage != null)
        {
            result = result with { StatusMessage = update.StatusMessage };
        }

        if (update.AvatarUrl != null)
        {
            result = result with { AvatarUrl = update.AvatarUrl };
        }

        _logger.LogDebug("Applied profile update for player {PlayerId}", profile.PlayerId);
        return result;
    }

    /// <summary>
    /// Updates player online status.
    /// </summary>
    public PlayerProfile UpdateStatus(PlayerProfile profile, PlayerOnlineStatus status, string? activity)
    {
        var result = profile with { Status = status, CurrentActivity = activity };
        _logger.LogDebug("Updated status for player {PlayerId} to {Status}", profile.PlayerId, status);
        return result;
    }

    /// <summary>
    /// Gets online players from a collection of profiles.
    /// </summary>
    public IReadOnlyList<OnlinePlayer> GetOnlinePlayers(
        IEnumerable<PlayerProfile> profiles,
        string? region = null)
    {
        var onlinePlayers = profiles
            .Where(p => p.Status == PlayerOnlineStatus.Online)
            .Where(p => region == null || p.Region == region)
            .Select(p => new OnlinePlayer
            {
                PlayerId = p.PlayerId,
                PlayerName = p.PlayerName,
                CurrentActivity = p.CurrentActivity,
                LastSeen = DateTime.UtcNow
            })
            .ToList();

        _logger.LogDebug("Found {Count} online players", onlinePlayers.Count);
        return onlinePlayers;
    }

    /// <summary>
    /// Creates a default player profile.
    /// </summary>
    public PlayerProfile CreateDefaultProfile(
        string playerId,
        string playerName,
        string region)
    {
        return new PlayerProfile(
            PlayerId: playerId,
            PlayerName: playerName,
            Rating: 1000,
            Rank: "Rookie",
            Achievements: new List<Achievement>(),
            Stats: new PlayerStats(0, 0, 0, 0, TimeSpan.Zero, new Dictionary<string, CharacterSpecificStats>()),
            Reputation: new Reputation(1000, ReputationTier.Neutral, new List<string>(), DateTime.UtcNow),
            FavoriteCharacters: Array.Empty<string>(),
            StatusMessage: "Ready to fight!",
            AvatarUrl: null,
            Status: PlayerOnlineStatus.Online,
            CurrentActivity: null,
            Region: region
        );
    }
}
