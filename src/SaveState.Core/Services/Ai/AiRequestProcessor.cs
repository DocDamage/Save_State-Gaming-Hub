using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Emotion;
using SaveState.Core.Services.Ai.Orchestration;
using SaveState.Core.Services.GameState;
using SaveState.Core.Services.Player;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Core AI request processor that orchestrates the complete request pipeline.
    /// </summary>
    public class AiRequestProcessor : IAiRequestProcessor
    {
        private readonly ILlmService _llmService;
        private readonly IIntentClassifier _intentClassifier;
        private readonly IAgentRouter _agentRouter;
        private readonly IAiMemoryCoordinator _memoryCoordinator;
        private readonly IAiWorldStateCoordinator _worldStateCoordinator;
        private readonly IAiValidationCoordinator _validationCoordinator;
        private readonly IAiEventCoordinator _eventCoordinator;
        private readonly IEmotionTagger _emotionTagger;
        private readonly AdvancedAiConfig _config;

        public AiRequestProcessor(
            ILlmService llmService,
            IIntentClassifier intentClassifier,
            IAgentRouter agentRouter,
            IAiMemoryCoordinator memoryCoordinator,
            IAiWorldStateCoordinator worldStateCoordinator,
            IAiValidationCoordinator validationCoordinator,
            IAiEventCoordinator eventCoordinator,
            IEmotionTagger emotionTagger,
            AdvancedAiConfig config)
        {
            _llmService = llmService;
            _intentClassifier = intentClassifier;
            _agentRouter = agentRouter;
            _memoryCoordinator = memoryCoordinator;
            _worldStateCoordinator = worldStateCoordinator;
            _validationCoordinator = validationCoordinator;
            _eventCoordinator = eventCoordinator;
            _emotionTagger = emotionTagger;
            _config = config;
        }

        public async Task<AiResponse> ProcessAsync(string input, AiRequestContext? context = null)
        {
            context ??= new AiRequestContext();
            var response = new AiResponse();

            // 1. Classify intent
            var intentContext = new Dictionary<string, object>();
            if (context.CurrentScene != null) intentContext["scene"] = context.CurrentScene;
            if (context.CurrentQuest != null) intentContext["quest"] = context.CurrentQuest;

            var intent = await _intentClassifier.ClassifyAsync(input, intentContext);
            response.Intent = intent.PrimaryIntent;

            // 2. Build context from memory
            var memoryContext = await _memoryCoordinator.BuildMemoryContextAsync(input, context.RelevantCharacters);

            // 3. Build routing context for agent selection
            var routingContext = new Dictionary<string, object>()
            {
                ["session_id"] = context.SessionId ?? Guid.NewGuid().ToString(),
                ["scene"] = context.CurrentScene ?? "",
                ["lore"] = memoryContext.CanonicalContext ?? ""
            };

            // Add world state to routing context
            if (_config.EnableStateInjection)
            {
                var worldState = _worldStateCoordinator.GetCurrentWorldState();
                foreach (var flag in worldState.Flags)
                    routingContext[$"flag_{flag.Key}"] = flag.Value;
                foreach (var counter in worldState.Counters)
                    routingContext[$"counter_{counter.Key}"] = counter.Value;
            }

            // 4. Route to select the best agent
            var routeDecision = await _agentRouter.RouteAsync(input, routingContext);

            // 5. Generate response using the selected agent's system prompt
            var agentSystemPrompt = routeDecision.SelectedAgent.SystemPrompt;
            var llmResponse = await _llmService.CompleteAsync(input, agentSystemPrompt);
            response.Content = llmResponse;
            response.Agent = routeDecision.SelectedAgent.AgentId;

            // 6. Validate and score
            var (validatedContent, wasValidated, confidence, validationMetadata) =
                await _validationCoordinator.ValidateAndScoreAsync(response.Content, context, _config);

            response.Content = validatedContent;
            response.WasValidated = wasValidated;
            response.Confidence = confidence;
            foreach (var kvp in validationMetadata)
            {
                response.Metadata[kvp.Key] = kvp.Value;
            }

            // 7. Record interaction in memory
            if (_config.EnableMemoryOrchestration)
            {
                await _memoryCoordinator.RecordInteractionAsync(input, response.Content, context.RequestType);
            }

            // 8. Tag emotion and publish event
            var emotion = _emotionTagger.Tag(response.Content);
            response.Metadata["emotion"] = emotion.PrimaryEmotion;

            await _eventCoordinator.PublishResponseEventAsync(response, response.Intent, emotion.PrimaryEmotion);

            return response;
        }
    }
}
