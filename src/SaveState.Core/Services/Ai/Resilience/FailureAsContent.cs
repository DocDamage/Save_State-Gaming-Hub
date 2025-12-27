using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Resilience
{
    /// <summary>
    /// Turns AI failures into story elements rather than breaking immersion.
    /// When AI fails, it shouldn't break immersion - it should become content.
    /// </summary>
    public interface IFailureAsContent
    {
        /// <summary>
        /// Wrap a failure in narrative
        /// </summary>
        NarrativeFailure WrapFailure(AiFailure failure);

        /// <summary>
        /// Get a graceful degradation response
        /// </summary>
        string GetGracefulDegradation(FailureContext context);

        /// <summary>
        /// Create an unreliable narrator response
        /// </summary>
        UnreliableNarratorResponse CreateUnreliableResponse(string topic, double reliability);

        /// <summary>
        /// Get a "missing data" narrative wrapper
        /// </summary>
        string WrapMissingData(string dataType, string context);

        /// <summary>
        /// Register a failure pattern
        /// </summary>
        void RegisterPattern(FailurePattern pattern);
    }

    /// <summary>
    /// An AI failure that needs wrapping
    /// </summary>
    public class AiFailure
    {
        public FailureType Type { get; set; }
        public string OriginalError { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string? RequestedContent { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Types of failures
    /// </summary>
    public enum FailureType
    {
        Timeout,
        RateLimit,
        ModelError,
        ContentFilter,
        ContextOverflow,
        NetworkError,
        ParseError,
        HallucinationDetected,
        LoreViolation,
        Unknown
    }

    /// <summary>
    /// Context for failure handling
    /// </summary>
    public class FailureContext
    {
        public string CurrentScene { get; set; } = string.Empty;
        public string FailureDescription { get; set; } = string.Empty;
        public bool IsDialogue { get; set; }
        public bool IsCombat { get; set; }
        public bool IsExploration { get; set; }
        public string? NpcId { get; set; }
    }

    /// <summary>
    /// A failure wrapped in narrative
    /// </summary>
    public class NarrativeFailure
    {
        public string NarrativeWrapper { get; set; } = string.Empty;
        public string FallbackContent { get; set; } = string.Empty;
        public bool PreservesImmersion { get; set; } = true;
        public FailureNarrativeType NarrativeType { get; set; }
        public bool SuggestsRetry { get; set; }
        public string? RetryPrompt { get; set; }
    }

    /// <summary>
    /// Types of narrative wrappers for failures
    /// </summary>
    public enum FailureNarrativeType
    {
        CorruptedArchives,      // "The records are damaged..."
        LostRecords,            // "This knowledge has been lost..."
        ConflictingTestimony,   // "Accounts differ on this..."
        UnreliableNarrator,     // "Or so the stories say..."
        MysteriousSilence,      // "Some things are better left unsaid..."
        TemporalDistortion,     // "Time itself seems uncertain here..."
        FadedMemory,            // "The details have grown hazy..."
        SecretKnowledge         // "This knowledge is forbidden..."
    }

    /// <summary>
    /// Response from unreliable narrator
    /// </summary>
    public class UnreliableNarratorResponse
    {
        public string PrimaryVersion { get; set; } = string.Empty;
        public List<string> AlternativeVersions { get; set; } = new();
        public string UncertaintyPhrase { get; set; } = string.Empty;
        public double StatedConfidence { get; set; }
        public bool AdmitsUncertainty { get; set; }
    }

    /// <summary>
    /// A pattern for handling specific failures
    /// </summary>
    public class FailurePattern
    {
        public string PatternId { get; set; } = Guid.NewGuid().ToString();
        public FailureType TriggerType { get; set; }
        public string? ContextMatch { get; set; }
        public FailureNarrativeType NarrativeType { get; set; }
        public List<string> Responses { get; set; } = new();
    }

    /// <summary>
    /// Default implementation of failure-as-content
    /// </summary>
    public class FailureAsContent : IFailureAsContent
    {
        private readonly ConcurrentDictionary<string, FailurePattern> _patterns = new();
        private readonly Random _random = new();

        public FailureAsContent()
        {
            RegisterDefaultPatterns();
        }

        public NarrativeFailure WrapFailure(AiFailure failure)
        {
            // Find matching pattern
            var pattern = _patterns.Values
                .FirstOrDefault(p => p.TriggerType == failure.Type);

            var narrativeType = pattern?.NarrativeType ?? GetDefaultNarrativeType(failure.Type);
            var wrapper = GetWrapperForType(narrativeType, failure.Context);
            var fallback = pattern?.Responses.Any() == true
                ? pattern.Responses[_random.Next(pattern.Responses.Count)]
                : GetDefaultFallback(narrativeType);

            return new NarrativeFailure
            {
                NarrativeWrapper = wrapper,
                FallbackContent = fallback,
                PreservesImmersion = true,
                NarrativeType = narrativeType,
                SuggestsRetry = failure.Type == FailureType.Timeout || failure.Type == FailureType.RateLimit,
                RetryPrompt = failure.Type == FailureType.Timeout 
                    ? "Give me a moment to recall..." 
                    : null
            };
        }

        public string GetGracefulDegradation(FailureContext context)
        {
            if (context.IsDialogue && context.NpcId != null)
            {
                return PickRandom(new[]
                {
                    "*pauses thoughtfully*",
                    "Hmm... where was I?",
                    "*trails off momentarily*",
                    "Forgive me, my thoughts wander.",
                    "*a distant look crosses their face*"
                });
            }

            if (context.IsCombat)
            {
                return PickRandom(new[]
                {
                    "The chaos of battle blurs the moment...",
                    "Steel clashes, impossibly fast!",
                    "The action unfolds in a blur!",
                    "Adrenaline pounds in your ears..."
                });
            }

            if (context.IsExploration)
            {
                return PickRandom(new[]
                {
                    "The way ahead is shrouded in uncertainty...",
                    "Something stirs in the shadows...",
                    "The path reveals itself slowly...",
                    "You sense there is more to discover..."
                });
            }

            return "A moment passes...";
        }

        public UnreliableNarratorResponse CreateUnreliableResponse(string topic, double reliability)
        {
            var uncertaintyPhrases = new[]
            {
                "Or so the legends say...",
                "Though some dispute this account...",
                "As far as anyone knows...",
                "If the old tales are true...",
                "History records, though memory fades...",
                "The truth, as ever, remains elusive..."
            };

            var response = new UnreliableNarratorResponse
            {
                UncertaintyPhrase = uncertaintyPhrases[_random.Next(uncertaintyPhrases.Length)],
                StatedConfidence = reliability,
                AdmitsUncertainty = reliability < 0.7
            };

            if (reliability < 0.5)
            {
                response.AlternativeVersions = new List<string>
                {
                    $"Some say {topic} was quite different...",
                    $"Others claim {topic} never happened at all.",
                    $"Ancient texts suggest an alternative truth..."
                };
            }

            return response;
        }

        public string WrapMissingData(string dataType, string context)
        {
            return dataType.ToLower() switch
            {
                "character" => $"Little is known about this figure. The {context} holds no records.",
                "location" => $"This place exists beyond the maps. The {context} remains uncharted.",
                "event" => $"No chronicle records this event. Perhaps it has been forgotten... or hidden.",
                "item" => $"The origins of this artifact are lost to time.",
                "lore" => $"The archives here are damaged. This knowledge has been lost.",
                _ => $"Some things remain mysteries. The {context} offers no answers."
            };
        }

        public void RegisterPattern(FailurePattern pattern)
        {
            _patterns[pattern.PatternId] = pattern;
        }

        private FailureNarrativeType GetDefaultNarrativeType(FailureType failureType)
        {
            return failureType switch
            {
                FailureType.Timeout => FailureNarrativeType.FadedMemory,
                FailureType.RateLimit => FailureNarrativeType.MysteriousSilence,
                FailureType.ContentFilter => FailureNarrativeType.SecretKnowledge,
                FailureType.ContextOverflow => FailureNarrativeType.LostRecords,
                FailureType.HallucinationDetected => FailureNarrativeType.ConflictingTestimony,
                FailureType.LoreViolation => FailureNarrativeType.CorruptedArchives,
                _ => FailureNarrativeType.UnreliableNarrator
            };
        }

        private string GetWrapperForType(FailureNarrativeType type, string context)
        {
            return type switch
            {
                FailureNarrativeType.CorruptedArchives =>
                    "The ancient texts here are damaged, the ink faded and crumbling...",
                FailureNarrativeType.LostRecords =>
                    "This knowledge has been lost to the ages. No record remains.",
                FailureNarrativeType.ConflictingTestimony =>
                    "Accounts differ on this matter. The truth remains elusive.",
                FailureNarrativeType.UnreliableNarrator =>
                    "Or so the stories say. Whether truth or legend, who can say?",
                FailureNarrativeType.MysteriousSilence =>
                    "Some knowledge is better left unspoken...",
                FailureNarrativeType.TemporalDistortion =>
                    "Time itself seems uncertain in this place...",
                FailureNarrativeType.FadedMemory =>
                    "The details have grown hazy, like a half-remembered dream...",
                FailureNarrativeType.SecretKnowledge =>
                    "This knowledge is forbidden. Some truths are too dangerous to speak.",
                _ => "The moment passes, its meaning unclear..."
            };
        }

        private string GetDefaultFallback(FailureNarrativeType type)
        {
            return type switch
            {
                FailureNarrativeType.CorruptedArchives => "...the rest is illegible.",
                FailureNarrativeType.LostRecords => "The search yields nothing.",
                FailureNarrativeType.ConflictingTestimony => "Perhaps the truth lies somewhere between.",
                FailureNarrativeType.UnreliableNarrator => "But you've learned not to trust every tale.",
                FailureNarrativeType.MysteriousSilence => "The silence speaks volumes.",
                FailureNarrativeType.TemporalDistortion => "The moment shifts, uncertain.",
                FailureNarrativeType.FadedMemory => "Perhaps it will come back to you later.",
                FailureNarrativeType.SecretKnowledge => "Some doors should remain closed.",
                _ => "..."
            };
        }

        private string PickRandom(string[] options)
        {
            return options[_random.Next(options.Length)];
        }

        private void RegisterDefaultPatterns()
        {
            // Timeout during dialogue
            RegisterPattern(new FailurePattern
            {
                TriggerType = FailureType.Timeout,
                ContextMatch = "dialogue",
                NarrativeType = FailureNarrativeType.FadedMemory,
                Responses = new()
                {
                    "*pauses, lost in thought*",
                    "Where was I? My mind wanders...",
                    "*gazes into the distance momentarily*"
                }
            });

            // Rate limit during exploration
            RegisterPattern(new FailurePattern
            {
                TriggerType = FailureType.RateLimit,
                ContextMatch = "exploration",
                NarrativeType = FailureNarrativeType.MysteriousSilence,
                Responses = new()
                {
                    "The path ahead requires patience...",
                    "Some discoveries take time to reveal themselves.",
                    "The world holds its secrets close."
                }
            });

            // Content filter in any context
            RegisterPattern(new FailurePattern
            {
                TriggerType = FailureType.ContentFilter,
                NarrativeType = FailureNarrativeType.SecretKnowledge,
                Responses = new()
                {
                    "Some knowledge is forbidden, even to speak of.",
                    "This truth is too dangerous to share.",
                    "The words catch in their throat, unsaid."
                }
            });

            // Lore violation
            RegisterPattern(new FailurePattern
            {
                TriggerType = FailureType.LoreViolation,
                NarrativeType = FailureNarrativeType.ConflictingTestimony,
                Responses = new()
                {
                    "Wait... that doesn't match the records. Let me think...",
                    "The histories disagree on this point.",
                    "Strange. The truth seems to shift like sand."
                }
            });

            // Hallucination detected
            RegisterPattern(new FailurePattern
            {
                TriggerType = FailureType.HallucinationDetected,
                NarrativeType = FailureNarrativeType.UnreliableNarrator,
                Responses = new()
                {
                    "Or perhaps I have the details wrong...",
                    "Though my memory may deceive me.",
                    "But can you truly trust what I say?"
                }
            });
        }
    }
}
