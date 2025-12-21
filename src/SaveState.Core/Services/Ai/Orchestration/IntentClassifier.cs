using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Orchestration
{
    /// <summary>
    /// Classifies incoming requests by type.
    /// Categories: Narrative, Combat, Emotional, SystemDesign, CodeGen, Lore, Economy, Quest
    /// </summary>
    public enum IntentCategory
    {
        Narrative,      // Storytelling, dialog, descriptions
        Combat,         // Battle, tactics, action sequences
        Emotional,      // Character feelings, relationships
        SystemDesign,   // Game mechanics, rules explanations
        CodeGen,        // Code generation, technical
        Lore,           // World knowledge, history
        Economy,        // Trading, resources, crafting
        Quest,          // Quest progression, objectives
        Tutorial,       // Help, guidance, tips
        Social,         // NPC interactions, dialogue
        Exploration,    // World navigation, discovery
        Meta,           // Game settings, UI, controls
        Unknown         // Cannot determine
    }

    public class IntentClassification
    {
        public IntentCategory PrimaryIntent { get; set; }
        public float Confidence { get; set; }
        public List<IntentCategory> SecondaryIntents { get; set; } = new();
        public Dictionary<string, float> AllScores { get; set; } = new();
        public List<string> ExtractedEntities { get; set; } = new();
        public string? DetectedTone { get; set; }
        public bool RequiresContext { get; set; }
        public bool RequiresValidation { get; set; }
    }

    public interface IIntentClassifier
    {
        Task<IntentClassification> ClassifyAsync(string input, Dictionary<string, object>? context = null);
        void AddCustomPattern(IntentCategory category, string pattern, float weight = 1.0f);
    }

    public class IntentClassifier : IIntentClassifier
    {
        private readonly Dictionary<IntentCategory, List<(string Pattern, float Weight)>> _patterns = new();
        private readonly Dictionary<IntentCategory, HashSet<string>> _keywords = new();
        private readonly Dictionary<string, IntentCategory> _entityTypeHints = new();

        public IntentClassifier()
        {
            InitializePatterns();
            InitializeKeywords();
            InitializeEntityHints();
        }

        private void InitializePatterns()
        {
            foreach (IntentCategory category in Enum.GetValues<IntentCategory>())
            {
                _patterns[category] = new List<(string, float)>();
            }

            // Narrative patterns
            _patterns[IntentCategory.Narrative].AddRange(new[]
            {
                (@"tell\s+(?:me\s+)?(?:a\s+)?story", 1.2f),
                (@"what\s+happens?\s+(?:next|if)", 1.0f),
                (@"describe\s+(?:the|this)", 0.9f),
                (@"narrate", 1.0f),
                (@"continue\s+(?:the\s+)?story", 1.1f)
            });

            // Combat patterns
            _patterns[IntentCategory.Combat].AddRange(new[]
            {
                (@"attack|fight|battle|combat", 1.0f),
                (@"damage|hit\s+points?|hp", 0.9f),
                (@"weapon|armor|shield", 0.8f),
                (@"enemy|monster|boss", 0.9f),
                (@"defend|block|parry", 0.9f)
            });

            // Emotional patterns
            _patterns[IntentCategory.Emotional].AddRange(new[]
            {
                (@"how\s+(?:does|do)\s+\w+\s+feel", 1.1f),
                (@"relationship|feelings?|emotions?", 1.0f),
                (@"love|hate|fear|angry", 0.9f),
                (@"trust|betray|loyalty", 0.9f)
            });

            // Lore patterns
            _patterns[IntentCategory.Lore].AddRange(new[]
            {
                (@"who\s+(?:is|was|are)", 0.9f),
                (@"what\s+(?:is|are)\s+the\s+history", 1.1f),
                (@"tell\s+me\s+about", 0.8f),
                (@"lore|legend|myth|history", 1.0f),
                (@"ancient|origin|creation", 0.8f)
            });

            // Quest patterns
            _patterns[IntentCategory.Quest].AddRange(new[]
            {
                (@"quest|mission|objective", 1.0f),
                (@"what\s+(?:should|do)\s+i\s+do\s+(?:next)?", 0.9f),
                (@"where\s+(?:should|do)\s+i\s+go", 0.8f),
                (@"complete|finish|accomplish", 0.7f)
            });

            // Economy patterns
            _patterns[IntentCategory.Economy].AddRange(new[]
            {
                (@"buy|sell|trade|merchant", 1.0f),
                (@"gold|coins?|currency|money", 0.9f),
                (@"price|cost|worth|value", 0.8f),
                (@"craft|forge|create\s+item", 0.9f),
                (@"inventory|items?|equipment", 0.8f)
            });

            // SystemDesign patterns
            _patterns[IntentCategory.SystemDesign].AddRange(new[]
            {
                (@"how\s+(?:does|do)\s+\w+\s+work", 0.9f),
                (@"mechanic|system|rule", 1.0f),
                (@"explain\s+(?:the\s+)?(?:game)?", 0.8f),
                (@"stats?|attributes?|skills?", 0.8f)
            });

            // Tutorial patterns
            _patterns[IntentCategory.Tutorial].AddRange(new[]
            {
                (@"help|assist|guide", 0.9f),
                (@"how\s+(?:to|do\s+i)", 1.0f),
                (@"tutorial|learn|teach", 1.0f),
                (@"tips?|hints?|advice", 0.9f)
            });

            // Social patterns
            _patterns[IntentCategory.Social].AddRange(new[]
            {
                (@"talk\s+to|speak\s+(?:with|to)", 1.0f),
                (@"npc|character|person", 0.8f),
                (@"dialogue|conversation|chat", 0.9f),
                (@"greet|introduce|meet", 0.8f)
            });

            // Exploration patterns
            _patterns[IntentCategory.Exploration].AddRange(new[]
            {
                (@"explore|discover|find", 0.9f),
                (@"where\s+is|location|map", 0.9f),
                (@"travel|go\s+to|visit", 0.8f),
                (@"area|region|zone|dungeon", 0.8f)
            });

            // Meta patterns
            _patterns[IntentCategory.Meta].AddRange(new[]
            {
                (@"settings?|options?|preferences?", 1.0f),
                (@"save|load|quit", 1.0f),
                (@"volume|controls?|keybinds?", 0.9f),
                (@"pause|menu|ui", 0.8f)
            });

            // CodeGen patterns
            _patterns[IntentCategory.CodeGen].AddRange(new[]
            {
                (@"generate\s+code|write\s+(?:a\s+)?script", 1.2f),
                (@"create\s+(?:a\s+)?function|implement", 1.0f),
                (@"code|script|program|algorithm", 0.8f),
                (@"debug|fix\s+(?:the\s+)?bug", 0.9f)
            });
        }

        private void InitializeKeywords()
        {
            _keywords[IntentCategory.Narrative] = new HashSet<string>
            { "story", "tale", "narrative", "plot", "chapter", "scene", "describe", "narrate" };

            _keywords[IntentCategory.Combat] = new HashSet<string>
            { "fight", "attack", "battle", "combat", "weapon", "damage", "enemy", "hit", "strike", "defend" };

            _keywords[IntentCategory.Emotional] = new HashSet<string>
            { "feel", "emotion", "love", "hate", "fear", "angry", "sad", "happy", "relationship", "trust" };

            _keywords[IntentCategory.Lore] = new HashSet<string>
            { "lore", "history", "legend", "myth", "ancient", "origin", "world", "kingdom", "realm" };

            _keywords[IntentCategory.Quest] = new HashSet<string>
            { "quest", "mission", "task", "objective", "goal", "complete", "find", "retrieve", "deliver" };

            _keywords[IntentCategory.Economy] = new HashSet<string>
            { "buy", "sell", "trade", "gold", "coins", "merchant", "shop", "craft", "forge", "price" };

            _keywords[IntentCategory.SystemDesign] = new HashSet<string>
            { "mechanic", "system", "rule", "stats", "level", "experience", "skill", "ability", "class" };

            _keywords[IntentCategory.Tutorial] = new HashSet<string>
            { "help", "how", "tutorial", "guide", "learn", "teach", "tip", "hint", "explain" };

            _keywords[IntentCategory.Social] = new HashSet<string>
            { "talk", "speak", "npc", "character", "dialogue", "conversation", "meet", "greet" };

            _keywords[IntentCategory.Exploration] = new HashSet<string>
            { "explore", "discover", "find", "location", "map", "travel", "journey", "area", "region" };

            _keywords[IntentCategory.Meta] = new HashSet<string>
            { "settings", "save", "load", "quit", "menu", "options", "controls", "volume", "pause" };

            _keywords[IntentCategory.CodeGen] = new HashSet<string>
            { "code", "script", "function", "program", "implement", "debug", "algorithm", "generate" };
        }

        private void InitializeEntityHints()
        {
            _entityTypeHints["sword"] = IntentCategory.Combat;
            _entityTypeHints["shield"] = IntentCategory.Combat;
            _entityTypeHints["armor"] = IntentCategory.Combat;
            _entityTypeHints["dragon"] = IntentCategory.Combat;
            _entityTypeHints["monster"] = IntentCategory.Combat;
            _entityTypeHints["boss"] = IntentCategory.Combat;
            _entityTypeHints["gold"] = IntentCategory.Economy;
            _entityTypeHints["shop"] = IntentCategory.Economy;
            _entityTypeHints["merchant"] = IntentCategory.Economy;
            _entityTypeHints["quest"] = IntentCategory.Quest;
            _entityTypeHints["map"] = IntentCategory.Exploration;
            _entityTypeHints["settings"] = IntentCategory.Meta;
        }

        public Task<IntentClassification> ClassifyAsync(string input, Dictionary<string, object>? context = null)
        {
            var inputLower = input.ToLowerInvariant();
            var scores = new Dictionary<IntentCategory, float>();

            // Initialize scores
            foreach (IntentCategory category in Enum.GetValues<IntentCategory>())
            {
                scores[category] = 0f;
            }

            // Pattern matching
            foreach (var categoryPatterns in _patterns)
            {
                foreach (var (pattern, weight) in categoryPatterns.Value)
                {
                    if (Regex.IsMatch(inputLower, pattern, RegexOptions.IgnoreCase))
                    {
                        scores[categoryPatterns.Key] += weight;
                    }
                }
            }

            // Keyword matching
            var words = inputLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                foreach (var categoryKeywords in _keywords)
                {
                    if (categoryKeywords.Value.Contains(word))
                    {
                        scores[categoryKeywords.Key] += 0.3f;
                    }
                }
            }

            // Entity-based hints
            var extractedEntities = new List<string>();
            foreach (var word in words)
            {
                if (_entityTypeHints.TryGetValue(word, out var hintCategory))
                {
                    scores[hintCategory] += 0.5f;
                    extractedEntities.Add(word);
                }
            }

            // Context boosting
            if (context != null)
            {
                if (context.TryGetValue("currentActivity", out var activity))
                {
                    var activityStr = activity.ToString()?.ToLowerInvariant() ?? "";
                    if (activityStr.Contains("combat")) scores[IntentCategory.Combat] += 0.5f;
                    if (activityStr.Contains("dialogue")) scores[IntentCategory.Social] += 0.5f;
                    if (activityStr.Contains("explore")) scores[IntentCategory.Exploration] += 0.5f;
                }
            }

            // Normalize and find top intents
            var maxScore = scores.Values.Max();
            var normalizedScores = scores.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => maxScore > 0 ? kvp.Value / maxScore : 0f
            );

            var sortedCategories = scores
                .OrderByDescending(kvp => kvp.Value)
                .ToList();

            var primaryIntent = sortedCategories[0].Key;
            var primaryScore = sortedCategories[0].Value;
            var confidence = maxScore > 0 ? Math.Min(1.0f, primaryScore / 2.0f) : 0f;

            // Get secondary intents (those within 50% of primary score)
            var secondaryIntents = sortedCategories
                .Skip(1)
                .Where(kvp => kvp.Value > primaryScore * 0.5f)
                .Select(kvp => kvp.Key)
                .Take(2)
                .ToList();

            // Detect tone
            var tone = DetectTone(inputLower);

            // Determine if validation/context is needed
            var requiresValidation = primaryIntent == IntentCategory.Combat || 
                                    primaryIntent == IntentCategory.Quest ||
                                    primaryIntent == IntentCategory.Economy;
            var requiresContext = primaryIntent == IntentCategory.Narrative ||
                                 primaryIntent == IntentCategory.Social ||
                                 primaryIntent == IntentCategory.Emotional;

            var result = new IntentClassification
            {
                PrimaryIntent = primaryIntent,
                Confidence = confidence,
                SecondaryIntents = secondaryIntents,
                AllScores = normalizedScores,
                ExtractedEntities = extractedEntities,
                DetectedTone = tone,
                RequiresContext = requiresContext,
                RequiresValidation = requiresValidation
            };

            return Task.FromResult(result);
        }

        public void AddCustomPattern(IntentCategory category, string pattern, float weight = 1.0f)
        {
            if (!_patterns.ContainsKey(category))
            {
                _patterns[category] = new List<(string, float)>();
            }
            _patterns[category].Add((pattern, weight));
        }

        private string DetectTone(string input)
        {
            if (input.Contains('!') || input.Contains("urgent") || input.Contains("hurry"))
                return "urgent";
            if (input.Contains('?'))
                return "inquisitive";
            if (input.Contains("please") || input.Contains("kindly"))
                return "polite";
            if (input.Contains("damn") || input.Contains("hell") || input.Contains("angry"))
                return "frustrated";
            return "neutral";
        }
    }
}
