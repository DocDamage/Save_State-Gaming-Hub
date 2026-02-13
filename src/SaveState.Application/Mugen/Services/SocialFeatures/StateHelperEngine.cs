using SaveState.Application.Mugen.Models.SocialFeatures;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.NetworkFeatures;

namespace SaveState.Application.Mugen.Services.SocialFeatures;

/// <summary>
/// Engine for state management helpers.
/// </summary>
public sealed class StateHelperEngine
{
    private readonly ILogger<StateHelperEngine> _logger;

    public StateHelperEngine(ILogger<StateHelperEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a friendship to a player's friendship list.
    /// </summary>
    public void AddFriendshipToList(Dictionary<string, List<Friendship>> friendships, string playerId, Friendship friendship)
    {
        if (!friendships.ContainsKey(playerId))
        {
            friendships[playerId] = new List<Friendship>();
        }
        friendships[playerId].Add(friendship);
    }

    /// <summary>
    /// Adds a message to chat history.
    /// </summary>
    public void AddMessageToHistory(
        Dictionary<string, List<Models.NetworkFeatures.ChatMessage>> chatHistory,
        string conversationId,
        Models.NetworkFeatures.ChatMessage message)
    {
        if (!chatHistory.ContainsKey(conversationId))
        {
            chatHistory[conversationId] = new List<Models.NetworkFeatures.ChatMessage>();
        }
        chatHistory[conversationId].Add(message);
    }

    /// <summary>
    /// Initializes sample player data.
    /// </summary>
    public void InitializeSamplePlayers(
        Dictionary<string, Models.NetworkFeatures.PlayerProfile> playerProfiles,
        Dictionary<string, List<Friendship>> friendships,
        ProfileEngine profileEngine)
    {
        var players = new[]
        {
            ("player1", "StreetFighterFan", "NA East"),
            ("player2", "ComboMaster", "EU West"),
            ("player3", "CharacterCreator", "Asia"),
            ("player4", "TournamentPro", "NA West"),
            ("player5", "CasualGamer", "EU Central")
        };

        foreach (var (id, name, region) in players)
        {
            playerProfiles[id] = profileEngine.CreateDefaultProfile(id, name, region);
            friendships[id] = new List<Friendship>();
        }
    }
}
