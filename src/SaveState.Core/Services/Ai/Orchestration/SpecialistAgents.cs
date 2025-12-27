using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Orchestration
{
    public interface ISpecialistAgent
    {
        string AgentId { get; }
        string Name { get; }
        Task<string> ProcessAsync(string input, AgentContext context);
        string BuildSystemPrompt(AgentContext context);
        bool CanHandle(IntentClassification intent);
    }

    public class AgentContext
    {
        public string SessionId { get; set; } = string.Empty;
        public string CurrentScene { get; set; } = string.Empty;
        public Dictionary<string, object> WorldState { get; set; } = new();
        public Dictionary<string, object> PlayerState { get; set; } = new();
        public List<string> RecentMemories { get; set; } = new();
        public string? CanonicalLore { get; set; }
        public IntentClassification? Intent { get; set; }
        public Dictionary<string, object> AdditionalContext { get; set; } = new();
    }

    public abstract class BaseSpecialistAgent : ISpecialistAgent
    {
        public abstract string AgentId { get; }
        public abstract string Name { get; }
        protected abstract string BaseSystemPrompt { get; }
        protected abstract IntentCategory[] HandledIntents { get; }
        protected readonly ILlmService _llmService;

        protected BaseSpecialistAgent(ILlmService? llmService = null)
        {
            _llmService = llmService ?? new LlmService();
        }

        public virtual async Task<string> ProcessAsync(string input, AgentContext context)
        {
            var systemPrompt = BuildSystemPrompt(context);
            return await _llmService.CompleteAsync(input, systemPrompt);
        }

        public virtual string BuildSystemPrompt(AgentContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine(BaseSystemPrompt);
            if (context.WorldState.Count > 0)
            {
                sb.AppendLine("\n=== World State ===");
                foreach (var (key, value) in context.WorldState)
                    sb.AppendLine($"- {key}: {value}");
            }
            if (!string.IsNullOrEmpty(context.CanonicalLore))
                sb.AppendLine(context.CanonicalLore);
            return sb.ToString();
        }

        public virtual bool CanHandle(IntentClassification intent) =>
            HandledIntents.Contains(intent.PrimaryIntent);
    }

    public class NarrativeAgent : BaseSpecialistAgent
    {
        public override string AgentId => "narrative";
        public override string Name => "Narrative Agent";
        protected override IntentCategory[] HandledIntents => new[] { IntentCategory.Narrative, IntentCategory.Social };
        protected override string BaseSystemPrompt => "You are a master storyteller. Write vivid, immersive narratives.";
        public NarrativeAgent(ILlmService? llm = null) : base(llm) { }
    }

    public class CombatAgent : BaseSpecialistAgent
    {
        public override string AgentId => "combat";
        public override string Name => "Combat Agent";
        protected override IntentCategory[] HandledIntents => new[] { IntentCategory.Combat };
        protected override string BaseSystemPrompt => "You narrate tactical combat. Never contradict rule engine validations.";
        public CombatAgent(ILlmService? llm = null) : base(llm) { }
    }

    public class LoreAgent : BaseSpecialistAgent
    {
        public override string AgentId => "lore";
        public override string Name => "Lore Agent";
        protected override IntentCategory[] HandledIntents => new[] { IntentCategory.Lore };
        protected override string BaseSystemPrompt => "You are the keeper of world knowledge. Only state canonical facts.";
        public LoreAgent(ILlmService? llm = null) : base(llm) { }
    }

    public class SystemAgent : BaseSpecialistAgent
    {
        public override string AgentId => "system";
        public override string Name => "System Agent";
        protected override IntentCategory[] HandledIntents => new[] { IntentCategory.SystemDesign, IntentCategory.Tutorial };
        protected override string BaseSystemPrompt => "You explain game mechanics precisely with accurate numbers.";
        public SystemAgent(ILlmService? llm = null) : base(llm) { }
    }

    public class EmotionAgent : BaseSpecialistAgent
    {
        public override string AgentId => "emotion";
        public override string Name => "Emotion Agent";
        protected override IntentCategory[] HandledIntents => new[] { IntentCategory.Emotional };
        protected override string BaseSystemPrompt => "You portray authentic character emotions and relationships.";
        public EmotionAgent(ILlmService? llm = null) : base(llm) { }
    }

    public class SpecialistAgentFactory
    {
        private readonly ILlmService _llmService;
        public SpecialistAgentFactory(ILlmService? llm = null) { _llmService = llm ?? new LlmService(); }
        
        public ISpecialistAgent? CreateAgent(string agentId) => agentId switch
        {
            "narrative" => new NarrativeAgent(_llmService),
            "combat" => new CombatAgent(_llmService),
            "lore" => new LoreAgent(_llmService),
            "system" => new SystemAgent(_llmService),
            "emotion" => new EmotionAgent(_llmService),
            _ => null
        };
    }
}
