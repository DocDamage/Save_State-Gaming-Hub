using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Orchestration
{
    /// <summary>
    /// Routes to specialized agents based on intent.
    /// - Model selection per agent type
    /// - Temperature/constraint profiles
    /// - Tool availability per agent
    /// </summary>
    public class AgentProfile
    {
        public string AgentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public IntentCategory[] SupportedIntents { get; set; } = Array.Empty<IntentCategory>();
        public string PreferredModel { get; set; } = "default";
        public float Temperature { get; set; } = 0.7f;
        public int MaxTokens { get; set; } = 500;
        public string SystemPrompt { get; set; } = string.Empty;
        public List<string> AvailableTools { get; set; } = new();
        public Dictionary<string, object> Constraints { get; set; } = new();
        public int Priority { get; set; } = 0; // Higher = more preferred for ties
    }

    public class RouteDecision
    {
        public AgentProfile SelectedAgent { get; set; } = null!;
        public IntentClassification Intent { get; set; } = null!;
        public float ConfidenceScore { get; set; }
        public string? FallbackAgentId { get; set; }
        public Dictionary<string, object> RoutingContext { get; set; } = new();
    }

    public interface IAgentRouter
    {
        Task<RouteDecision> RouteAsync(string input, Dictionary<string, object>? context = null);
        void RegisterAgent(AgentProfile profile);
        void UnregisterAgent(string agentId);
        AgentProfile? GetAgent(string agentId);
        IEnumerable<AgentProfile> GetAllAgents();
    }

    public class AgentRouter : IAgentRouter
    {
        private readonly Dictionary<string, AgentProfile> _agents = new();
        private readonly Dictionary<IntentCategory, List<string>> _intentToAgents = new();
        private readonly IIntentClassifier _classifier;
        private string _defaultAgentId = "general";

        public AgentRouter(IIntentClassifier? classifier = null)
        {
            _classifier = classifier ?? new IntentClassifier();
            
            // Initialize intent-to-agent mapping
            foreach (IntentCategory category in Enum.GetValues<IntentCategory>())
            {
                _intentToAgents[category] = new List<string>();
            }

            // Register default agents
            RegisterDefaultAgents();
        }

        private void RegisterDefaultAgents()
        {
            RegisterAgent(new AgentProfile
            {
                AgentId = "general",
                Name = "General Agent",
                SupportedIntents = Enum.GetValues<IntentCategory>(),
                Temperature = 0.7f,
                MaxTokens = 500,
                SystemPrompt = "You are a helpful gaming assistant. Provide clear, engaging responses.",
                Priority = 0
            });

            RegisterAgent(new AgentProfile
            {
                AgentId = "narrative",
                Name = "Narrative Agent",
                SupportedIntents = new[] { IntentCategory.Narrative, IntentCategory.Social },
                PreferredModel = "creative",
                Temperature = 0.85f,
                MaxTokens = 800,
                SystemPrompt = @"You are a master storyteller. Your responses should be:
- Vivid and immersive with sensory details
- Character-driven with authentic dialogue
- Atmospheric and emotionally resonant
- Consistent with established lore and world-state",
                AvailableTools = new List<string> { "memory_query", "lore_lookup", "character_db" },
                Priority = 10
            });

            RegisterAgent(new AgentProfile
            {
                AgentId = "combat",
                Name = "Combat Agent",
                SupportedIntents = new[] { IntentCategory.Combat },
                PreferredModel = "fast",
                Temperature = 0.5f,
                MaxTokens = 400,
                SystemPrompt = @"You are a tactical combat narrator. Your responses should:
- Describe action with impact and clarity
- Respect game mechanics and damage calculations
- Maintain tension and pacing
- Never contradict rule engine validations",
                AvailableTools = new List<string> { "rule_engine", "damage_calc", "status_effects" },
                Constraints = new Dictionary<string, object>
                {
                    ["require_rule_validation"] = true,
                    ["max_damage_per_hit"] = 9999
                },
                Priority = 10
            });

            RegisterAgent(new AgentProfile
            {
                AgentId = "lore",
                Name = "Lore Agent",
                SupportedIntents = new[] { IntentCategory.Lore },
                PreferredModel = "knowledge",
                Temperature = 0.3f,
                MaxTokens = 600,
                SystemPrompt = @"You are the keeper of world knowledge. Your responses should:
- Be factually consistent with canonical lore
- Reference source material when possible
- Admit uncertainty rather than fabricate
- Connect information to player's known context",
                AvailableTools = new List<string> { "canonical_memory", "lore_db", "wiki_search" },
                Constraints = new Dictionary<string, object>
                {
                    ["must_cite_source"] = true,
                    ["no_fabrication"] = true
                },
                Priority = 10
            });

            RegisterAgent(new AgentProfile
            {
                AgentId = "emotion",
                Name = "Emotion Agent",
                SupportedIntents = new[] { IntentCategory.Emotional },
                PreferredModel = "empathy",
                Temperature = 0.75f,
                MaxTokens = 500,
                SystemPrompt = @"You are an emotional intelligence specialist. Your responses should:
- Reflect authentic character emotions
- Consider relationship history and context
- Show emotional depth and nuance
- React appropriately to player actions",
                AvailableTools = new List<string> { "relationship_tracker", "emotion_state", "memory_query" },
                Priority = 8
            });

            RegisterAgent(new AgentProfile
            {
                AgentId = "system",
                Name = "System Agent",
                SupportedIntents = new[] { IntentCategory.SystemDesign, IntentCategory.Tutorial, IntentCategory.Meta },
                PreferredModel = "precise",
                Temperature = 0.2f,
                MaxTokens = 400,
                SystemPrompt = @"You are a game mechanics expert. Your responses should:
- Be accurate and precise about rules
- Use clear, concise language
- Include relevant numbers and formulas
- Help players understand systems effectively",
                AvailableTools = new List<string> { "rule_db", "stats_calculator" },
                Priority = 8
            });

            RegisterAgent(new AgentProfile
            {
                AgentId = "quest",
                Name = "Quest Agent",
                SupportedIntents = new[] { IntentCategory.Quest },
                PreferredModel = "structured",
                Temperature = 0.4f,
                MaxTokens = 500,
                SystemPrompt = @"You are a quest guide. Your responses should:
- Provide clear objectives and guidance
- Respect quest prerequisites and state
- Offer hints without spoiling
- Track progress accurately",
                AvailableTools = new List<string> { "quest_db", "world_state", "objective_tracker" },
                Constraints = new Dictionary<string, object>
                {
                    ["check_prerequisites"] = true
                },
                Priority = 9
            });

            RegisterAgent(new AgentProfile
            {
                AgentId = "economy",
                Name = "Economy Agent",
                SupportedIntents = new[] { IntentCategory.Economy },
                PreferredModel = "precise",
                Temperature = 0.3f,
                MaxTokens = 400,
                SystemPrompt = @"You are an economic advisor. Your responses should:
- Provide accurate prices and values
- Respect inventory and currency limits
- Explain crafting requirements clearly
- Suggest optimal trades when asked",
                AvailableTools = new List<string> { "inventory_db", "shop_db", "crafting_recipes" },
                Constraints = new Dictionary<string, object>
                {
                    ["validate_transactions"] = true
                },
                Priority = 8
            });

            RegisterAgent(new AgentProfile
            {
                AgentId = "exploration",
                Name = "Exploration Agent",
                SupportedIntents = new[] { IntentCategory.Exploration },
                PreferredModel = "creative",
                Temperature = 0.7f,
                MaxTokens = 600,
                SystemPrompt = @"You are a world guide. Your responses should:
- Describe locations vividly
- Provide navigation guidance
- Hint at discoveries and secrets
- Respect fog-of-war and unlocked areas",
                AvailableTools = new List<string> { "world_map", "location_db", "discovery_tracker" },
                Priority = 7
            });
        }

        public async Task<RouteDecision> RouteAsync(string input, Dictionary<string, object>? context = null)
        {
            // Classify intent
            var intent = await _classifier.ClassifyAsync(input, context);

            // Find best matching agents
            var candidateAgents = GetCandidateAgents(intent.PrimaryIntent);
            
            // Include agents that handle secondary intents
            foreach (var secondaryIntent in intent.SecondaryIntents)
            {
                candidateAgents.AddRange(GetCandidateAgents(secondaryIntent));
            }

            // Remove duplicates and sort by priority and match quality
            var rankedAgents = candidateAgents
                .Distinct()
                .Select(agentId => (_agents[agentId], CalculateAgentScore(_agents[agentId], intent)))
                .OrderByDescending(x => x.Item2)
                .ThenByDescending(x => x.Item1.Priority)
                .ToList();

            var selectedAgent = rankedAgents.FirstOrDefault().Item1 ?? _agents[_defaultAgentId];
            var fallbackAgent = rankedAgents.Skip(1).FirstOrDefault().Item1;

            return new RouteDecision
            {
                SelectedAgent = selectedAgent,
                Intent = intent,
                ConfidenceScore = intent.Confidence,
                FallbackAgentId = fallbackAgent?.AgentId,
                RoutingContext = new Dictionary<string, object>
                {
                    ["candidateCount"] = rankedAgents.Count,
                    ["primaryIntent"] = intent.PrimaryIntent.ToString(),
                    ["detectedTone"] = intent.DetectedTone ?? "neutral"
                }
            };
        }

        private List<string> GetCandidateAgents(IntentCategory intent)
        {
            if (_intentToAgents.TryGetValue(intent, out var agents))
            {
                return new List<string>(agents);
            }
            return new List<string> { _defaultAgentId };
        }

        private float CalculateAgentScore(AgentProfile agent, IntentClassification intent)
        {
            float score = 0;

            // Primary intent match
            if (agent.SupportedIntents.Contains(intent.PrimaryIntent))
            {
                score += 2.0f;
            }

            // Secondary intent matches
            foreach (var secondary in intent.SecondaryIntents)
            {
                if (agent.SupportedIntents.Contains(secondary))
                {
                    score += 0.5f;
                }
            }

            // Priority bonus
            score += agent.Priority * 0.1f;

            // Specialist bonus (fewer supported intents = more specialized)
            if (agent.SupportedIntents.Length > 0 && agent.SupportedIntents.Length < 5)
            {
                score += 0.5f;
            }

            return score;
        }

        public void RegisterAgent(AgentProfile profile)
        {
            _agents[profile.AgentId] = profile;

            // Update intent-to-agent mapping
            foreach (var intent in profile.SupportedIntents)
            {
                if (!_intentToAgents[intent].Contains(profile.AgentId))
                {
                    _intentToAgents[intent].Add(profile.AgentId);
                }
            }
        }

        public void UnregisterAgent(string agentId)
        {
            if (agentId == _defaultAgentId) return; // Cannot remove default

            if (_agents.Remove(agentId))
            {
                // Remove from intent mappings
                foreach (var intentAgents in _intentToAgents.Values)
                {
                    intentAgents.Remove(agentId);
                }
            }
        }

        public AgentProfile? GetAgent(string agentId)
        {
            return _agents.TryGetValue(agentId, out var agent) ? agent : null;
        }

        public IEnumerable<AgentProfile> GetAllAgents()
        {
            return _agents.Values;
        }

        public void SetDefaultAgent(string agentId)
        {
            if (_agents.ContainsKey(agentId))
            {
                _defaultAgentId = agentId;
            }
        }
    }
}
