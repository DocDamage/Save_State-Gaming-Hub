using System.Collections.Generic;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Prompts;
using SaveState.Core.Services.Player;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Generates specialized narrative and commentary content.
    /// </summary>
    public class AiNarrativeGenerator : IAiNarrativeGenerator
    {
        private readonly IAiRequestProcessor _requestProcessor;
        private readonly IPromptTemplateService _templateService;
        private readonly IPromptMutator _promptMutator;
        private readonly IPlayerModelService _playerModelService;
        private readonly AdvancedAiConfig _config;

        public AiNarrativeGenerator(
            IAiRequestProcessor requestProcessor,
            IPromptTemplateService templateService,
            IPromptMutator promptMutator,
            IPlayerModelService playerModelService,
            AdvancedAiConfig config)
        {
            _requestProcessor = requestProcessor;
            _templateService = templateService;
            _promptMutator = promptMutator;
            _playerModelService = playerModelService;
            _config = config;
        }

        public async Task<string> GenerateNarrativeAsync(string prompt, NarrativeContext? context = null)
        {
            context ??= new NarrativeContext();

            var variables = new Dictionary<string, object>
            {
                ["location"] = context.Location ?? "the scene",
                ["player_action"] = prompt,
                ["mood"] = context.Mood ?? "neutral",
                ["time_of_day"] = context.TimeOfDay ?? "day"
            };

            var narrativePrompt = _templateService.Render("narrative_scene", variables);

            var response = await _requestProcessor.ProcessAsync(narrativePrompt, new AiRequestContext
            {
                RequestType = "narrative",
                CurrentScene = context.Location
            });

            return response.Content;
        }

        public async Task<string> GenerateCommentaryAsync(string gameEvent, CommentaryContext? context = null)
        {
            context ??= new CommentaryContext();

            var playerProfile = await _playerModelService.GetProfile(_config.DefaultPlayerId);

            var prompt = $"Generate exciting live commentary for this gaming moment:\n" +
                        $"Event: {gameEvent}\n" +
                        $"Game: {context.GameTitle ?? "the game"}\n" +
                        $"Player action: {context.PlayerAction ?? "playing"}\n" +
                        (context.Score.HasValue ? $"Score: {context.Score}\n" : "") +
                        (context.Combo.HasValue ? $"Combo: {context.Combo}x\n" : "");

            // Mutate based on player preferences
            var mutatedPrompt = _promptMutator.Mutate(prompt, playerProfile);

            var response = await _requestProcessor.ProcessAsync(mutatedPrompt, new AiRequestContext
            {
                RequestType = "commentary",
                RequireValidation = false // Commentary doesn't need strict validation
            });

            return response.Content;
        }
    }
}
