using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Persona
{
    /// <summary>
    /// Dynamic persona management for NPCs and AI entities.
    /// Personas evolve based on world state - characters don't reset every scene.
    /// </summary>
    public interface IPersonaHotSwapper
    {
        /// <summary>
        /// Get current persona for an entity
        /// </summary>
        PersonaState GetPersonaState(string entityId);

        /// <summary>
        /// Trigger a persona transition
        /// </summary>
        Task<PersonaTransitionResult> TransitionAsync(string entityId, PersonaTransitionRequest request);

        /// <summary>
        /// Register a persona definition
        /// </summary>
        void RegisterPersona(PersonaDefinition persona);

        /// <summary>
        /// Register transition rules
        /// </summary>
        void RegisterTransitionRule(PersonaTransitionRule rule);

        /// <summary>
        /// Evaluate world state and update personas automatically
        /// </summary>
        Task EvaluateWorldStateAsync(WorldStateSnapshot worldState);

        /// <summary>
        /// Get persona history for an entity
        /// </summary>
        IEnumerable<PersonaHistoryEntry> GetPersonaHistory(string entityId);
    }

    /// <summary>
    /// A persona state enum
    /// </summary>
    public enum PersonaType
    {
        Calm,           // Default baseline
        Friendly,       // Positive disposition
        Suspicious,     // Wary of player
        Paranoid,       // After betrayal detected
        Hostile,        // Actively antagonistic
        Broken,         // After traumatic event
        Corrupted,      // Influenced by dark force
        Hopeful,        // After positive resolution
        Grieving,       // After loss
        Manic,          // Unstable, erratic
        Stoic,          // Emotionally guarded
        Terrified,      // Fear-driven
        Reverent,       // Awed by player
        Dismissive,     // Uninterested
        Romantic        // Attracted to player
    }

    /// <summary>
    /// Current persona state for an entity
    /// </summary>
    public class PersonaState
    {
        public string EntityId { get; set; } = string.Empty;
        public PersonaType CurrentPersona { get; set; } = PersonaType.Calm;
        public double Intensity { get; set; } = 0.5; // 0-1, how strongly expressed
        public DateTime EnteredAt { get; set; } = DateTime.UtcNow;
        public string? TriggeringEvent { get; set; }
        public Dictionary<string, double> EmotionalModifiers { get; set; } = new();
        public List<PersonaType> RecentPersonas { get; set; } = new();
        public bool IsLocked { get; set; } = false; // Cannot transition if locked
    }

    /// <summary>
    /// Definition of a persona
    /// </summary>
    public class PersonaDefinition
    {
        public PersonaType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string SystemPromptModifier { get; set; } = string.Empty;
        public Dictionary<string, double> BehaviorModifiers { get; set; } = new();
        public List<string> DialoguePatterns { get; set; } = new();
        public List<string> AvoidedTopics { get; set; } = new();
        public List<PersonaType> AllowedTransitionsTo { get; set; } = new();
    }

    /// <summary>
    /// Request to transition a persona
    /// </summary>
    public class PersonaTransitionRequest
    {
        public PersonaType TargetPersona { get; set; }
        public string TriggerEvent { get; set; } = string.Empty;
        public double? TargetIntensity { get; set; }
        public bool Force { get; set; } = false;
        public Dictionary<string, object> Context { get; set; } = new();
    }

    /// <summary>
    /// Result of a persona transition
    /// </summary>
    public class PersonaTransitionResult
    {
        public bool Success { get; set; }
        public PersonaType? PreviousPersona { get; set; }
        public PersonaType NewPersona { get; set; }
        public string? Reason { get; set; }
        public string? NarrativeDescription { get; set; }
    }

    /// <summary>
    /// Rule for automatic persona transitions
    /// </summary>
    public class PersonaTransitionRule
    {
        public string RuleId { get; set; } = Guid.NewGuid().ToString();
        public string TriggerEvent { get; set; } = string.Empty;
        public List<PersonaType>? FromPersonas { get; set; } // null = any
        public PersonaType ToPersona { get; set; }
        public Func<WorldStateSnapshot, string, bool>? Condition { get; set; }
        public double MinimumIntensity { get; set; } = 0.5;
        public string NarrativeTemplate { get; set; } = string.Empty;
    }

    /// <summary>
    /// Snapshot of world state for persona evaluation
    /// </summary>
    public class WorldStateSnapshot
    {
        public Dictionary<string, object> Flags { get; set; } = new();
        public Dictionary<string, double> Relationships { get; set; } = new();
        public List<string> RecentEvents { get; set; } = new();
        public string? CurrentLocation { get; set; }
        public DateTime GameTime { get; set; }
    }

    /// <summary>
    /// History entry for persona changes
    /// </summary>
    public class PersonaHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public PersonaType FromPersona { get; set; }
        public PersonaType ToPersona { get; set; }
        public string TriggerEvent { get; set; } = string.Empty;
        public string? NarrativeNote { get; set; }
    }

    /// <summary>
    /// Default implementation of persona hot-swapper
    /// </summary>
    public class PersonaHotSwapper : IPersonaHotSwapper
    {
        private readonly ConcurrentDictionary<string, PersonaState> _entityPersonas = new();
        private readonly ConcurrentDictionary<PersonaType, PersonaDefinition> _definitions = new();
        private readonly ConcurrentDictionary<string, List<PersonaTransitionRule>> _eventRules = new();
        private readonly ConcurrentDictionary<string, List<PersonaHistoryEntry>> _history = new();

        public PersonaHotSwapper()
        {
            RegisterDefaultDefinitions();
            RegisterDefaultRules();
        }

        public PersonaState GetPersonaState(string entityId)
        {
            return _entityPersonas.GetOrAdd(entityId, _ => new PersonaState { EntityId = entityId });
        }

        public async Task<PersonaTransitionResult> TransitionAsync(string entityId, PersonaTransitionRequest request)
        {
            var currentState = GetPersonaState(entityId);

            // Check if locked
            if (currentState.IsLocked && !request.Force)
            {
                return new PersonaTransitionResult
                {
                    Success = false,
                    Reason = "Persona is locked and cannot transition"
                };
            }

            // Check if transition is allowed
            if (_definitions.TryGetValue(currentState.CurrentPersona, out var currentDef))
            {
                if (currentDef.AllowedTransitionsTo.Any() && 
                    !currentDef.AllowedTransitionsTo.Contains(request.TargetPersona) &&
                    !request.Force)
                {
                    return new PersonaTransitionResult
                    {
                        Success = false,
                        Reason = $"Cannot transition from {currentState.CurrentPersona} to {request.TargetPersona}"
                    };
                }
            }

            // Record history
            var historyEntry = new PersonaHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                FromPersona = currentState.CurrentPersona,
                ToPersona = request.TargetPersona,
                TriggerEvent = request.TriggerEvent,
                NarrativeNote = await GenerateTransitionNarrative(
                    currentState.CurrentPersona, request.TargetPersona, request.TriggerEvent)
            };

            _history.AddOrUpdate(
                entityId,
                new List<PersonaHistoryEntry> { historyEntry },
                (_, list) => { list.Add(historyEntry); return list; });

            // Update state
            var previousPersona = currentState.CurrentPersona;
            currentState.RecentPersonas.Add(previousPersona);
            if (currentState.RecentPersonas.Count > 5)
            {
                currentState.RecentPersonas.RemoveAt(0);
            }

            currentState.CurrentPersona = request.TargetPersona;
            currentState.Intensity = request.TargetIntensity ?? 0.5;
            currentState.EnteredAt = DateTime.UtcNow;
            currentState.TriggeringEvent = request.TriggerEvent;

            return new PersonaTransitionResult
            {
                Success = true,
                PreviousPersona = previousPersona,
                NewPersona = request.TargetPersona,
                NarrativeDescription = historyEntry.NarrativeNote
            };
        }

        public void RegisterPersona(PersonaDefinition persona)
        {
            _definitions[persona.Type] = persona;
        }

        public void RegisterTransitionRule(PersonaTransitionRule rule)
        {
            _eventRules.AddOrUpdate(
                rule.TriggerEvent,
                new List<PersonaTransitionRule> { rule },
                (_, list) => { list.Add(rule); return list; });
        }

        public async Task EvaluateWorldStateAsync(WorldStateSnapshot worldState)
        {
            foreach (var recentEvent in worldState.RecentEvents)
            {
                if (_eventRules.TryGetValue(recentEvent, out var rules))
                {
                    foreach (var rule in rules)
                    {
                        foreach (var entityId in _entityPersonas.Keys)
                        {
                            var currentState = GetPersonaState(entityId);

                            // Check from-persona constraint
                            if (rule.FromPersonas != null && 
                                !rule.FromPersonas.Contains(currentState.CurrentPersona))
                            {
                                continue;
                            }

                            // Check condition
                            if (rule.Condition != null && 
                                !rule.Condition(worldState, entityId))
                            {
                                continue;
                            }

                            // Execute transition
                            await TransitionAsync(entityId, new PersonaTransitionRequest
                            {
                                TargetPersona = rule.ToPersona,
                                TriggerEvent = recentEvent,
                                TargetIntensity = rule.MinimumIntensity
                            });
                        }
                    }
                }
            }
        }

        public IEnumerable<PersonaHistoryEntry> GetPersonaHistory(string entityId)
        {
            return _history.TryGetValue(entityId, out var history) 
                ? history 
                : Enumerable.Empty<PersonaHistoryEntry>();
        }

        private Task<string> GenerateTransitionNarrative(
            PersonaType from, PersonaType to, string trigger)
        {
            var templates = new Dictionary<(PersonaType, PersonaType), string>
            {
                { (PersonaType.Calm, PersonaType.Paranoid), "A shadow of doubt crosses their face." },
                { (PersonaType.Calm, PersonaType.Friendly), "Their demeanor warms visibly." },
                { (PersonaType.Friendly, PersonaType.Broken), "Something breaks behind their eyes." },
                { (PersonaType.Calm, PersonaType.Hostile), "Their expression hardens dangerously." },
                { (PersonaType.Hostile, PersonaType.Calm), "The tension slowly drains away." },
                { (PersonaType.Calm, PersonaType.Grieving), "Sorrow etches itself into their features." },
                { (PersonaType.Paranoid, PersonaType.Hostile), "Suspicion gives way to open hostility." },
                { (PersonaType.Broken, PersonaType.Hopeful), "A spark of hope returns to their eyes." }
            };

            if (templates.TryGetValue((from, to), out var template))
            {
                return Task.FromResult(template);
            }

            return Task.FromResult($"Their demeanor shifts from {from.ToString().ToLower()} to {to.ToString().ToLower()}.");
        }

        private void RegisterDefaultDefinitions()
        {
            RegisterPersona(new PersonaDefinition
            {
                Type = PersonaType.Calm,
                Description = "Neutral, balanced state",
                SystemPromptModifier = "Respond in a calm, measured manner.",
                AllowedTransitionsTo = Enum.GetValues<PersonaType>().ToList()
            });

            RegisterPersona(new PersonaDefinition
            {
                Type = PersonaType.Paranoid,
                Description = "Suspicious, sees threats everywhere",
                SystemPromptModifier = "Be suspicious of everything. Question motives. Trust no one.",
                BehaviorModifiers = new() { ["trust"] = -0.5, ["honesty"] = -0.3 },
                DialoguePatterns = new() { "Are you sure about that?", "I'm watching you.", "Something's not right here." },
                AllowedTransitionsTo = new() { PersonaType.Hostile, PersonaType.Broken, PersonaType.Calm }
            });

            RegisterPersona(new PersonaDefinition
            {
                Type = PersonaType.Broken,
                Description = "Emotionally shattered, traumatized",
                SystemPromptModifier = "Respond from a place of deep trauma. Be fragile, fragmented, distant.",
                BehaviorModifiers = new() { ["engagement"] = -0.7, ["hope"] = -0.8 },
                DialoguePatterns = new() { "It doesn't matter anymore.", "I can't...", "Please, just go." },
                AllowedTransitionsTo = new() { PersonaType.Hopeful, PersonaType.Calm }
            });

            RegisterPersona(new PersonaDefinition
            {
                Type = PersonaType.Corrupted,
                Description = "Influenced by dark forces",
                SystemPromptModifier = "Something dark speaks through you. Subtle malevolence colors your words.",
                BehaviorModifiers = new() { ["morality"] = -0.8, ["stability"] = -0.5 },
                DialoguePatterns = new() { "The darkness shows me truths...", "Join us.", "You don't understand power." },
                AllowedTransitionsTo = new() { PersonaType.Hostile, PersonaType.Broken }
            });

            RegisterPersona(new PersonaDefinition
            {
                Type = PersonaType.Hopeful,
                Description = "Renewed optimism after hardship",
                SystemPromptModifier = "Speak with cautious optimism. The future seems brighter.",
                BehaviorModifiers = new() { ["engagement"] = 0.5, ["trust"] = 0.3 },
                AllowedTransitionsTo = new() { PersonaType.Calm, PersonaType.Friendly }
            });
        }

        private void RegisterDefaultRules()
        {
            // Betrayal triggers paranoia
            RegisterTransitionRule(new PersonaTransitionRule
            {
                TriggerEvent = "PLAYER_BETRAYAL",
                ToPersona = PersonaType.Paranoid,
                MinimumIntensity = 0.7,
                NarrativeTemplate = "After the betrayal, {entity} regards you with deep suspicion."
            });

            // Loss of loved one triggers grief
            RegisterTransitionRule(new PersonaTransitionRule
            {
                TriggerEvent = "LOVED_ONE_DIED",
                ToPersona = PersonaType.Grieving,
                MinimumIntensity = 0.9,
                NarrativeTemplate = "Grief overwhelms {entity}."
            });

            // Repeated kindness builds trust
            RegisterTransitionRule(new PersonaTransitionRule
            {
                TriggerEvent = "PLAYER_HELPED_NPC",
                FromPersonas = new() { PersonaType.Suspicious, PersonaType.Calm },
                ToPersona = PersonaType.Friendly,
                MinimumIntensity = 0.6
            });

            // Dark exposure corrupts
            RegisterTransitionRule(new PersonaTransitionRule
            {
                TriggerEvent = "EXPOSED_TO_CORRUPTION",
                ToPersona = PersonaType.Corrupted,
                Condition = (world, entity) => 
                    world.Flags.TryGetValue("corruption_level", out var level) && 
                    level is double d && d > 0.5
            });
        }
    }
}
