using System.Collections.Generic;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Prompts;
using SaveState.Core.Services.GameState;
using SaveState.Core.Services.Player;
using SaveState.Core.Services.Timeline;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Coordinates world state and player model management.
    /// </summary>
    public class AiWorldStateCoordinator : IAiWorldStateCoordinator
    {
        private readonly IWorldStateService _worldStateService;
        private readonly IPlayerModelService _playerModelService;
        private readonly IBehaviorTracker _behaviorTracker;
        private readonly AdvancedAiConfig _config;

        public AiWorldStateCoordinator(
            IWorldStateService worldStateService,
            IPlayerModelService playerModelService,
            IBehaviorTracker behaviorTracker,
            AdvancedAiConfig config)
        {
            _worldStateService = worldStateService;
            _playerModelService = playerModelService;
            _behaviorTracker = behaviorTracker;
            _config = config;
        }

        public void UpdateWorldState(string key, object value, string? source = null)
        {
            if (value is bool boolVal)
                _worldStateService.SetFlag(key, boolVal, source);
            else if (value is int intVal)
                _worldStateService.SetCounter(key, intVal, source);
            else
                _worldStateService.SetRelation(key, value.ToString() ?? "", source);
        }

        public WorldState GetCurrentWorldState() => _worldStateService.CurrentState;

        public async Task UpdatePlayerModelAsync(PlayerAction action)
        {
            _behaviorTracker.TrackAction(action);
            await _playerModelService.UpdateFromAction(_config.DefaultPlayerId, action);
        }

        public async Task<PlayerProfile> GetPlayerProfileAsync(string playerId)
        {
            return await _playerModelService.GetProfile(playerId);
        }
    }
}
