using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Memory;
using SaveState.Core.Services.GameState;

namespace SaveState.Core.Services.Ai.Orchestration
{
    public interface IIntentRouter
    {
        Task<string> RouteAndProcessAsync(string input, string sessionId, string userId);
    }

    public class IntentRouter : IIntentRouter
    {
        private readonly IEnhancedIntentClassifier _classifier;
        private readonly IEnumerable<ISpecialistAgent> _agents;
        private readonly IStateInjector _stateInjector;
        private readonly IEpisodicMemory _memory;
        private readonly ILoreLocker _loreLocker;
        private readonly IMemoryWriterService _memoryWriter; // Ensure subscription is active

        public IntentRouter(
            IEnhancedIntentClassifier classifier,
            IEnumerable<ISpecialistAgent> agents,
            IStateInjector stateInjector,
            IEpisodicMemory memory,
            ILoreLocker loreLocker,
            IMemoryWriterService memoryWriter)
        {
            _classifier = classifier;
            _agents = agents;
            _stateInjector = stateInjector;
            _memory = memory;
            _loreLocker = loreLocker;
            _memoryWriter = memoryWriter;
            
            // Ensure listeners are registered
            _memoryWriter.RegisterSubscription();
        }

        public async Task<string> RouteAndProcessAsync(string input, string sessionId, string userId)
        {
            // 1. Classify Intent
            var classification = await _classifier.ClassifyAsync(input);
            var primaryIntent = classification.PrimaryIntent;

            // 2. Select Agent
            var agent = _agents.FirstOrDefault(a => a.CanHandle(primaryIntent)) 
                        ?? _agents.FirstOrDefault(a => a.CanHandle(IntentCategory.Narrative))!; // Fallback

            // 3. Gather Context (RAG + State)
            var context = await BuildContextAsync(input, classification, sessionId, userId);

            // 4. Execute
            return await agent.ProcessAsync(input, context);
        }

        private async Task<AgentContext> BuildContextAsync(string input, EnhancedIntentClassification intent, string sessionId, string userId)
        {
            // A. Get deterministic world state
            var worldSnapshot = _stateInjector.GetSnapshot(new InjectionContext 
            { 
               // Default injection settings
            });

            // B. Get RAG Memories
            var memories = await _memory.SemanticSearch(input, maxResults: 5);

            // C. Get Relevant Lore (if applicable)
            var lore = new List<LockedLore>();
            if (intent.PrimaryIntent == IntentCategory.Lore || intent.PrimaryIntent == IntentCategory.Narrative)
            {
                // We'd use a semantic search on LoreLocker here if available
                // For now, we reuse the validation logic to find related lore
                var validation = await _loreLocker.ValidateAsync(input);
                if (validation.RelatedLore != null)
                {
                    lore.AddRange(validation.RelatedLore);
                }
            }

            return new AgentContext
            {
                SessionId = sessionId,
                UserId = userId,
                WorldState = worldSnapshot,
                RelevantMemories = memories.ToList(),
                RelevantLore = lore,
                Intent = new IntentClassification 
                { 
                    PrimaryIntent = intent.PrimaryIntent, 
                    Confidence = intent.PrimaryConfidence 
                },
                EnhancedIntent = intent
            };
        }
    }
}
