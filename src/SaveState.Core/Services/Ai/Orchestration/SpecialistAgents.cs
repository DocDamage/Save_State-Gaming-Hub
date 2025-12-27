using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Memory;
using SaveState.Core.Services.Rules;
using SaveState.Core.Services.GameState;

namespace SaveState.Core.Services.Ai.Orchestration
{
    public interface ISpecialistAgent
    {
        string AgentId { get; }
        string Name { get; }
        Task<string> ProcessAsync(string input, AgentContext context);
        bool CanHandle(IntentCategory intent);
    }

    public abstract class BaseSpecialistAgent : ISpecialistAgent
    {
        public abstract string AgentId { get; }
        public abstract string Name { get; }
        protected abstract string BaseSystemPrompt { get; }
        protected abstract IntentCategory[] HandledIntents { get; }
        protected readonly ILlmService _llmService;

        protected BaseSpecialistAgent(ILlmService llmService)
        {
            _llmService = llmService;
        }

        public virtual bool CanHandle(IntentCategory intent) =>
            HandledIntents.Contains(intent);

        public virtual async Task<string> ProcessAsync(string input, AgentContext context)
        {
            var systemPrompt = await BuildSystemPromptAsync(context);
            return await _llmService.CompleteAsync(input, systemPrompt);
        }

        protected virtual Task<string> BuildSystemPromptAsync(AgentContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine(BaseSystemPrompt);
            
            // Inject World State if available
            if (context.WorldState != null)
            {
                sb.AppendLine("\n=== CURRENT STATE ===");
                sb.AppendLine($"Location: {context.WorldState.RegionId} - {context.WorldState.SceneId}");
                sb.AppendLine($"Time: {context.WorldState.Timestamp}");
                
                if (context.WorldState.QuestFlags.Any())
                {
                    sb.AppendLine("Flags: " + string.Join(", ", context.WorldState.QuestFlags.Keys));
                }
            }

            // Inject Memories
            if (context.RelevantMemories.Any())
            {
                sb.AppendLine("\n=== RELEVANT MEMORIES ===");
                foreach (var mem in context.RelevantMemories.Take(5))
                {
                    sb.AppendLine($"- {mem.Event} -> {mem.Outcome}");
                }
            }

            return Task.FromResult(sb.ToString());
        }
    }

    /// <summary>
    /// Handles Storytelling, Dialogue, social interactions.
    /// Uses deeper memory access and personality injection.
    /// </summary>
    public class NarrativeSpecialist : BaseSpecialistAgent
    {
        public override string AgentId => "narrative_specialist";
        public override string Name => "Narrative Specialist";
        
        protected override IntentCategory[] HandledIntents => new[] 
        { 
            IntentCategory.Narrative, 
            IntentCategory.Social, 
            IntentCategory.Emotional,
            IntentCategory.Exploration 
        };

        protected override string BaseSystemPrompt => 
            "You are the Narrative Engine. Write vivid, immersive descriptions and authentic dialogue. " +
            "Focus on sensory details and character voice. Never contradict established World State.";

        public NarrativeSpecialist(ILlmService llmService) : base(llmService) { }
    }

    /// <summary>
    /// Handles Lore queries, history, myth, and background.
    /// Uses LoreLocker to ensure canon compliance.
    /// </summary>
    public class LoreSpecialist : BaseSpecialistAgent
    {
        private readonly ILoreLocker _loreLocker;

        public override string AgentId => "lore_specialist";
        public override string Name => "Lore Specialist";

        protected override IntentCategory[] HandledIntents => new[] { IntentCategory.Lore };
        
        protected override string BaseSystemPrompt => 
            "You are the Keeper of Archives. Provide accurate, canonical information about the world history and legends. " +
            "If information is missing, admit ignorance rather than hallucinating facts that contradict Canon.";

        public LoreSpecialist(ILlmService llmService, ILoreLocker loreLocker) : base(llmService)
        {
            _loreLocker = loreLocker;
        }

        protected override async Task<string> BuildSystemPromptAsync(AgentContext context)
        {
            var prompt = await base.BuildSystemPromptAsync(context);
            var sb = new StringBuilder(prompt);

            if (context.RelevantLore.Any())
            {
                sb.AppendLine("\n=== CANONICAL LORE (STRICT) ===");
                foreach (var lore in context.RelevantLore)
                {
                    sb.AppendLine($"- {lore.Statement} (Confidence: {lore.ConfidenceThreshold})");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Handles Game Mechanics, Combat Math, Economy, and Quests.
    /// Uses RulesEngine to ensure validity.
    /// </summary>
    public class SystemSpecialist : BaseSpecialistAgent
    {
        private readonly IRuleEngine _ruleEngine;

        public override string AgentId => "system_specialist";
        public override string Name => "System Specialist";

        protected override IntentCategory[] HandledIntents => new[] 
        { 
            IntentCategory.Combat, 
            IntentCategory.Economy, 
            IntentCategory.Quest, 
            IntentCategory.SystemDesign,
            IntentCategory.Tutorial,
            IntentCategory.Meta,
            IntentCategory.CodeGen
        };

        protected override string BaseSystemPrompt => 
            "You are the Game Master. Explain rules, calculate outcomes, and manage game mechanics clearly and precisely. " +
            "Use tables and bullet points for data. Ensure all advice matches the Rule Engine constraints.";

        public SystemSpecialist(ILlmService llmService, IRuleEngine ruleEngine) : base(llmService)
        {
            _ruleEngine = ruleEngine;
        }

        protected override Task<string> BuildSystemPromptAsync(AgentContext context)
        {
            // Future: Query RuleEngine for specific rules relevant to the context
            // For now, we rely on the base context
            return base.BuildSystemPromptAsync(context);
        }
    }
}
