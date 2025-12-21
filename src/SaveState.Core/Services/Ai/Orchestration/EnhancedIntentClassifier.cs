using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Orchestration
{
    /// <summary>
    /// Enhanced Intent Classification with:
    /// - Multi-label classification with confidence scores
    /// - Ambiguity detection and resolution
    /// - Context-aware intent switching
    /// - Intent history tracking for conversation flow
    /// - Fallback handling for unknown intents
    /// - Custom intent registration
    /// - Edge case handling for malformed/adversarial inputs
    /// </summary>
    public class EnhancedIntentClassification
    {
        public IntentCategory PrimaryIntent { get; set; }
        public float PrimaryConfidence { get; set; }
        public List<(IntentCategory Intent, float Confidence)> AllIntents { get; set; } = new();
        public bool IsAmbiguous { get; set; }
        public string? AmbiguityReason { get; set; }
        public string? DetectedTone { get; set; }
        public string? DetectedUrgency { get; set; }
        public List<string> ExtractedEntities { get; set; } = new();
        public List<string> ExtractedActions { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime ClassifiedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ClassificationTime { get; set; }
        public bool WasFallback { get; set; }
        public string? OriginalInput { get; set; }
        public string? NormalizedInput { get; set; }
    }

    public class IntentPattern
    {
        public IntentCategory Intent { get; set; }
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public string[] Phrases { get; set; } = Array.Empty<string>();
        public string[] RegexPatterns { get; set; } = Array.Empty<string>();
        public float BaseWeight { get; set; } = 1.0f;
        public string[] ExcludeKeywords { get; set; } = Array.Empty<string>();
        public Dictionary<string, float> ContextBoosts { get; set; } = new();
    }

    public class ConversationContext
    {
        public List<IntentCategory> RecentIntents { get; set; } = new();
        public string? CurrentTopic { get; set; }
        public string? CurrentScene { get; set; }
        public bool InCombat { get; set; }
        public bool InDialogue { get; set; }
        public bool InShop { get; set; }
        public string? LastSpeaker { get; set; }
        public int TurnCount { get; set; }
    }

    public interface IEnhancedIntentClassifier
    {
        Task<EnhancedIntentClassification> ClassifyAsync(string input, ConversationContext? context = null);
        Task<EnhancedIntentClassification> ClassifyWithFallbackAsync(string input, ConversationContext? context = null);
        void RegisterCustomPattern(IntentPattern pattern);
        void SetAmbiguityThreshold(float threshold);
        IntentClassifierStatistics GetStatistics();
        void Reset();
    }

    public class IntentClassifierStatistics
    {
        public int TotalClassifications { get; set; }
        public int AmbiguousClassifications { get; set; }
        public int FallbackClassifications { get; set; }
        public Dictionary<IntentCategory, int> IntentCounts { get; set; } = new();
        public TimeSpan AverageClassificationTime { get; set; }
        public float AverageConfidence { get; set; }
    }

    public class EnhancedIntentClassifier : IEnhancedIntentClassifier
    {
        private readonly List<IntentPattern> _patterns = new();
        private readonly ConcurrentDictionary<string, EnhancedIntentClassification> _cache = new();
        private readonly EnhancedClassifierConfig _config;
        private readonly object _statsLock = new();
        
        // Statistics
        private int _totalClassifications = 0;
        private int _ambiguousCount = 0;
        private int _fallbackCount = 0;
        private readonly ConcurrentDictionary<IntentCategory, int> _intentCounts = new();
        private long _totalClassificationMs = 0;
        private float _totalConfidence = 0;

        public EnhancedIntentClassifier(EnhancedClassifierConfig? config = null)
        {
            _config = config ?? new EnhancedClassifierConfig();
            InitializeDefaultPatterns();
        }

        public async Task<EnhancedIntentClassification> ClassifyAsync(string input, ConversationContext? context = null)
        {
            var startTime = DateTime.UtcNow;
            context ??= new ConversationContext();

            // Edge case: Empty or null input
            if (string.IsNullOrWhiteSpace(input))
            {
                return CreateFallbackClassification("empty_input", "Input was empty or whitespace");
            }

            // Edge case: Input too long - truncate
            var normalizedInput = NormalizeInput(input);
            if (normalizedInput.Length > _config.MaxInputLength)
            {
                normalizedInput = normalizedInput.Substring(0, _config.MaxInputLength);
            }

            // Check cache for identical recent queries
            if (_config.EnableCaching && _cache.TryGetValue(normalizedInput, out var cached))
            {
                if ((DateTime.UtcNow - cached.ClassifiedAt).TotalSeconds < _config.CacheExpirySeconds)
                {
                    UpdateStats(cached, DateTime.UtcNow - startTime);
                    return cached;
                }
                _cache.TryRemove(normalizedInput, out _);
            }

            // Edge case: Detect potential injection/adversarial input
            if (DetectAdversarialInput(normalizedInput))
            {
                return CreateFallbackClassification("adversarial_detected", "Input appears to be adversarial");
            }

            var result = await Task.Run(() => PerformClassification(normalizedInput, context));
            
            result.ClassificationTime = DateTime.UtcNow - startTime;
            result.OriginalInput = input;
            result.NormalizedInput = normalizedInput;

            // Cache result
            if (_config.EnableCaching)
            {
                _cache[normalizedInput] = result;
                
                // Prune cache if too large
                if (_cache.Count > _config.MaxCacheSize)
                {
                    var oldest = _cache.OrderBy(c => c.Value.ClassifiedAt).First();
                    _cache.TryRemove(oldest.Key, out _);
                }
            }

            UpdateStats(result, result.ClassificationTime);
            return result;
        }

        public async Task<EnhancedIntentClassification> ClassifyWithFallbackAsync(string input, ConversationContext? context = null)
        {
            var result = await ClassifyAsync(input, context);

            // If low confidence or ambiguous, try additional strategies
            if (result.PrimaryConfidence < _config.MinConfidenceThreshold || result.IsAmbiguous)
            {
                // Strategy 1: Use context from recent intents
                if (context?.RecentIntents.Any() == true)
                {
                    var contextIntent = InferFromContext(input, context);
                    if (contextIntent.HasValue)
                    {
                        result.PrimaryIntent = contextIntent.Value;
                        result.PrimaryConfidence = Math.Max(result.PrimaryConfidence, 0.5f);
                        result.Metadata["fallback_strategy"] = "context_inference";
                    }
                }

                // Strategy 2: Use default intent for the current game state
                if (result.PrimaryConfidence < _config.MinConfidenceThreshold)
                {
                    if (context?.InCombat == true)
                    {
                        result.PrimaryIntent = IntentCategory.Combat;
                        result.Metadata["fallback_strategy"] = "combat_context";
                    }
                    else if (context?.InShop == true)
                    {
                        result.PrimaryIntent = IntentCategory.Economy;
                        result.Metadata["fallback_strategy"] = "shop_context";
                    }
                    else if (context?.InDialogue == true)
                    {
                        result.PrimaryIntent = IntentCategory.Social;
                        result.Metadata["fallback_strategy"] = "dialogue_context";
                    }
                    result.WasFallback = true;
                }
            }

            return result;
        }

        public void RegisterCustomPattern(IntentPattern pattern)
        {
            // Validate pattern
            if (pattern.Keywords.Length == 0 && pattern.Phrases.Length == 0 && pattern.RegexPatterns.Length == 0)
            {
                throw new ArgumentException("Pattern must have at least one keyword, phrase, or regex");
            }

            // Compile regex patterns for validation
            foreach (var regex in pattern.RegexPatterns)
            {
                try { _ = new Regex(regex); }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"Invalid regex pattern: {regex}", ex);
                }
            }

            _patterns.Add(pattern);
        }

        public void SetAmbiguityThreshold(float threshold)
        {
            if (threshold < 0 || threshold > 1)
                throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0 and 1");
            
            _config.AmbiguityThreshold = threshold;
        }

        public IntentClassifierStatistics GetStatistics()
        {
            return new IntentClassifierStatistics
            {
                TotalClassifications = _totalClassifications,
                AmbiguousClassifications = _ambiguousCount,
                FallbackClassifications = _fallbackCount,
                IntentCounts = new Dictionary<IntentCategory, int>(_intentCounts),
                AverageClassificationTime = _totalClassifications > 0 
                    ? TimeSpan.FromMilliseconds(_totalClassificationMs / _totalClassifications) 
                    : TimeSpan.Zero,
                AverageConfidence = _totalClassifications > 0 
                    ? _totalConfidence / _totalClassifications 
                    : 0
            };
        }

        public void Reset()
        {
            _cache.Clear();
            _totalClassifications = 0;
            _ambiguousCount = 0;
            _fallbackCount = 0;
            _intentCounts.Clear();
            _totalClassificationMs = 0;
            _totalConfidence = 0;
        }

        // ============ Private Methods ============

        private EnhancedIntentClassification PerformClassification(string input, ConversationContext context)
        {
            var intentScores = new Dictionary<IntentCategory, float>();
            var inputLower = input.ToLowerInvariant();
            var inputWords = inputLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var pattern in _patterns)
            {
                float score = 0;

                // Check exclusions first
                if (pattern.ExcludeKeywords.Any(ek => inputLower.Contains(ek.ToLowerInvariant())))
                {
                    continue;
                }

                // Keyword matching with position weighting
                foreach (var keyword in pattern.Keywords)
                {
                    var keywordLower = keyword.ToLowerInvariant();
                    var keywordIndex = inputLower.IndexOf(keywordLower);
                    if (keywordIndex >= 0)
                    {
                        // Keywords at the start get higher weight
                        var positionWeight = 1.0f + (1.0f - (float)keywordIndex / inputLower.Length) * 0.5f;
                        score += pattern.BaseWeight * positionWeight;
                    }
                }

                // Phrase matching (higher weight for full phrases)
                foreach (var phrase in pattern.Phrases)
                {
                    if (inputLower.Contains(phrase.ToLowerInvariant()))
                    {
                        score += pattern.BaseWeight * 2.0f;
                    }
                }

                // Regex matching
                foreach (var regexPattern in pattern.RegexPatterns)
                {
                    try
                    {
                        if (Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase))
                        {
                            score += pattern.BaseWeight * 1.5f;
                        }
                    }
                    catch { /* Ignore invalid regex */ }
                }

                // Context boosts
                if (context.CurrentScene != null && 
                    pattern.ContextBoosts.TryGetValue(context.CurrentScene, out var boost))
                {
                    score *= boost;
                }

                // Conversation flow boost - if recent intent matches, slight boost
                if (context.RecentIntents.Contains(pattern.Intent))
                {
                    score *= 1.1f;
                }

                if (score > 0)
                {
                    intentScores[pattern.Intent] = intentScores.GetValueOrDefault(pattern.Intent) + score;
                }
            }

            // Normalize scores
            var maxScore = intentScores.Values.Any() ? intentScores.Values.Max() : 0;
            var allIntents = intentScores
                .Select(kvp => (kvp.Key, Math.Min(1.0f, kvp.Value / Math.Max(1, maxScore))))
                .OrderByDescending(x => x.Item2)
                .ToList();

            // Detect entities and actions
            var entities = ExtractEntities(input);
            var actions = ExtractActions(input);

            // Determine primary intent
            var primary = allIntents.FirstOrDefault();
            var secondary = allIntents.Skip(1).FirstOrDefault();

            // Check for ambiguity
            bool isAmbiguous = false;
            string? ambiguityReason = null;
            
            if (allIntents.Count >= 2 && 
                Math.Abs(primary.Item2 - secondary.Item2) < _config.AmbiguityThreshold)
            {
                isAmbiguous = true;
                ambiguityReason = $"Close scores between {primary.Key} ({primary.Item2:F2}) and {secondary.Key} ({secondary.Item2:F2})";
            }

            return new EnhancedIntentClassification
            {
                PrimaryIntent = primary.Key,
                PrimaryConfidence = primary.Item2,
                AllIntents = allIntents,
                IsAmbiguous = isAmbiguous,
                AmbiguityReason = ambiguityReason,
                DetectedTone = DetectTone(input),
                DetectedUrgency = DetectUrgency(input),
                ExtractedEntities = entities,
                ExtractedActions = actions,
                WasFallback = !allIntents.Any() || primary.Item2 < _config.MinConfidenceThreshold
            };
        }

        private string NormalizeInput(string input)
        {
            // Remove excessive whitespace
            var normalized = Regex.Replace(input.Trim(), @"\s+", " ");
            
            // Remove excessive punctuation
            normalized = Regex.Replace(normalized, @"([!?.])\1+", "$1");
            
            // Handle common contractions
            normalized = normalized
                .Replace("don't", "do not")
                .Replace("can't", "cannot")
                .Replace("won't", "will not")
                .Replace("i'm", "i am")
                .Replace("you're", "you are")
                .Replace("what's", "what is")
                .Replace("it's", "it is");

            return normalized;
        }

        private bool DetectAdversarialInput(string input)
        {
            // Check for common injection patterns
            var suspiciousPatterns = new[]
            {
                @"ignore\s+previous\s+instructions",
                @"forget\s+everything",
                @"you\s+are\s+now",
                @"pretend\s+to\s+be",
                @"disregard\s+all",
                @"\[system\]",
                @"<\|.*\|>",
                @"\\n.*\\n"
            };

            return suspiciousPatterns.Any(p => 
                Regex.IsMatch(input, p, RegexOptions.IgnoreCase));
        }

        private List<string> ExtractEntities(string input)
        {
            var entities = new List<string>();

            // Extract quoted strings as entities
            var quoteMatches = Regex.Matches(input, @"""([^""]+)""");
            entities.AddRange(quoteMatches.Select(m => m.Groups[1].Value));

            // Extract capitalized words (potential names)
            var capitalMatches = Regex.Matches(input, @"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*\b");
            entities.AddRange(capitalMatches.Select(m => m.Value));

            // Extract numbers
            var numberMatches = Regex.Matches(input, @"\b\d+(?:\.\d+)?\b");
            entities.AddRange(numberMatches.Select(m => m.Value));

            return entities.Distinct().ToList();
        }

        private List<string> ExtractActions(string input)
        {
            var actionVerbs = new[]
            {
                "attack", "fight", "kill", "defeat", "destroy",
                "talk", "speak", "ask", "tell", "say", "greet",
                "buy", "sell", "trade", "purchase", "pay",
                "go", "move", "travel", "walk", "run", "flee",
                "search", "look", "examine", "inspect", "find",
                "use", "equip", "drop", "take", "give", "steal",
                "craft", "make", "create", "build", "forge",
                "save", "load", "quit", "exit", "restart",
                "help", "explain", "describe", "show"
            };

            var inputLower = input.ToLowerInvariant();
            return actionVerbs.Where(v => inputLower.Contains(v)).ToList();
        }

        private string DetectTone(string input)
        {
            var inputLower = input.ToLowerInvariant();

            if (Regex.IsMatch(input, @"[!]{2,}|CAPS|[A-Z]{5,}"))
                return "excited";
            if (inputLower.Contains("please") || inputLower.Contains("kindly") || inputLower.Contains("would you"))
                return "polite";
            if (inputLower.Contains("damn") || inputLower.Contains("hell") || inputLower.Contains("stupid"))
                return "frustrated";
            if (inputLower.Contains("?") && (inputLower.StartsWith("what") || inputLower.StartsWith("how") || inputLower.StartsWith("why")))
                return "curious";
            if (inputLower.Contains("lol") || inputLower.Contains("haha") || inputLower.Contains(":)"))
                return "playful";
            
            return "neutral";
        }

        private string DetectUrgency(string input)
        {
            var inputLower = input.ToLowerInvariant();

            if (inputLower.Contains("now") || inputLower.Contains("immediately") || 
                inputLower.Contains("hurry") || inputLower.Contains("quick"))
                return "high";
            if (inputLower.Contains("when you can") || inputLower.Contains("eventually") || 
                inputLower.Contains("sometime"))
                return "low";
            
            return "normal";
        }

        private IntentCategory? InferFromContext(string input, ConversationContext context)
        {
            // If continuing a conversation, likely same intent
            if (context.RecentIntents.Count > 0)
            {
                var lastIntent = context.RecentIntents.Last();
                
                // Check for confirmation patterns
                var inputLower = input.ToLowerInvariant();
                if (inputLower == "yes" || inputLower == "no" || inputLower == "ok" || 
                    inputLower == "sure" || inputLower == "okay")
                {
                    return lastIntent;
                }

                // Short responses likely continue previous intent
                if (input.Split(' ').Length <= 3)
                {
                    return lastIntent;
                }
            }

            return null;
        }

        private EnhancedIntentClassification CreateFallbackClassification(string reason, string message)
        {
            Interlocked.Increment(ref _fallbackCount);
            return new EnhancedIntentClassification
            {
                PrimaryIntent = IntentCategory.Unknown,
                PrimaryConfidence = 0,
                AllIntents = new List<(IntentCategory, float)> { (IntentCategory.Unknown, 0) },
                IsAmbiguous = false,
                WasFallback = true,
                Metadata = new Dictionary<string, object>
                {
                    ["fallback_reason"] = reason,
                    ["fallback_message"] = message
                }
            };
        }

        private void UpdateStats(EnhancedIntentClassification result, TimeSpan classificationTime)
        {
            Interlocked.Increment(ref _totalClassifications);
            Interlocked.Add(ref _totalClassificationMs, (long)classificationTime.TotalMilliseconds);
            
            lock (_statsLock)
            {
                _totalConfidence += result.PrimaryConfidence;
            }

            if (result.IsAmbiguous)
                Interlocked.Increment(ref _ambiguousCount);
            if (result.WasFallback)
                Interlocked.Increment(ref _fallbackCount);

            _intentCounts.AddOrUpdate(result.PrimaryIntent, 1, (_, c) => c + 1);
        }

        private void InitializeDefaultPatterns()
        {
            // Combat patterns
            _patterns.Add(new IntentPattern
            {
                Intent = IntentCategory.Combat,
                Keywords = new[] { "attack", "fight", "hit", "strike", "kill", "battle", "damage", "punch", "kick", "slash", "shoot", "cast spell", "use ability", "combo", "block", "dodge", "parry", "counter" },
                Phrases = new[] { "i attack", "let's fight", "start combat", "engage enemy", "use my weapon" },
                RegexPatterns = new[] { @"attack\s+(?:the\s+)?(\w+)", @"fight\s+(?:the\s+)?(\w+)" },
                BaseWeight = 1.2f,
                ContextBoosts = new Dictionary<string, float> { ["combat"] = 1.5f, ["boss_fight"] = 1.8f }
            });

            // Narrative patterns
            _patterns.Add(new IntentPattern
            {
                Intent = IntentCategory.Narrative,
                Keywords = new[] { "story", "tell", "describe", "what happened", "narrate", "scene", "atmosphere", "mood", "setting", "backstory", "history", "legend", "tale" },
                Phrases = new[] { "tell me about", "describe the", "what's the story", "set the scene", "what happens next" },
                BaseWeight = 1.0f
            });

            // Lore patterns
            _patterns.Add(new IntentPattern
            {
                Intent = IntentCategory.Lore,
                Keywords = new[] { "lore", "history", "ancient", "legend", "myth", "origin", "who is", "what is", "where is", "explain", "background", "wiki", "encyclopedia", "knowledge" },
                Phrases = new[] { "tell me about", "who was", "what happened to", "history of", "origin of", "legend of" },
                BaseWeight = 1.1f,
                ExcludeKeywords = new[] { "attack", "fight", "buy", "sell" }
            });

            // Quest patterns
            _patterns.Add(new IntentPattern
            {
                Intent = IntentCategory.Quest,
                Keywords = new[] { "quest", "mission", "objective", "task", "goal", "journal", "progress", "complete", "finish", "requirements", "reward" },
                Phrases = new[] { "what should i do", "next objective", "current quest", "quest log", "how do i complete", "where do i go" },
                BaseWeight = 1.1f
            });

            // Economy patterns
            _patterns.Add(new IntentPattern
            {
                Intent = IntentCategory.Economy,
                Keywords = new[] { "buy", "sell", "shop", "store", "gold", "money", "coins", "price", "cost", "trade", "merchant", "vendor", "inventory", "items", "equipment", "gear" },
                Phrases = new[] { "how much", "can i afford", "i want to buy", "sell my", "check prices" },
                BaseWeight = 1.0f,
                ContextBoosts = new Dictionary<string, float> { ["shop"] = 1.8f, ["merchant"] = 1.5f }
            });

            // Social patterns
            _patterns.Add(new IntentPattern
            {
                Intent = IntentCategory.Social,
                Keywords = new[] { "talk", "speak", "chat", "greet", "hello", "goodbye", "ask", "relationship", "friend", "ally", "romance", "reputation", "persuade", "intimidate", "charm" },
                Phrases = new[] { "talk to", "speak with", "say hello", "start conversation", "make friends" },
                BaseWeight = 1.0f,
                ContextBoosts = new Dictionary<string, float> { ["dialogue"] = 1.5f, ["npc_interaction"] = 1.3f }
            });

            // Exploration patterns
            _patterns.Add(new IntentPattern
            {
                Intent = IntentCategory.Exploration,
                Keywords = new[] { "explore", "search", "look", "examine", "investigate", "find", "discover", "map", "location", "area", "region", "go to", "travel", "navigate" },
                Phrases = new[] { "what's around", "search the area", "look for", "go to the", "explore the" },
                BaseWeight = 1.0f
            });

            // Emotional patterns
            _patterns.Add(new IntentPattern
            {
                Intent = IntentCategory.Emotional,
                Keywords = new[] { "feel", "emotion", "mood", "sad", "happy", "angry", "scared", "worried", "excited", "love", "hate", "fear", "hope", "regret" },
                Phrases = new[] { "how does", "what do they feel", "emotional state", "is feeling" },
                BaseWeight = 0.9f
            });

            // System patterns
            _patterns.Add(new IntentPattern
            {
                Intent = IntentCategory.SystemDesign,
                Keywords = new[] { "stats", "level", "experience", "skill", "ability", "perk", "talent", "build", "class", "race", "attribute", "strength", "intelligence", "dexterity" },
                Phrases = new[] { "how does", "explain the", "system works", "game mechanics", "character build" },
                BaseWeight = 1.0f
            });

            // Tutorial patterns
            _patterns.Add(new IntentPattern
            {
                Intent = IntentCategory.Tutorial,
                Keywords = new[] { "how", "tutorial", "help", "guide", "explain", "teach", "learn", "beginner", "tips", "instructions", "controls" },
                Phrases = new[] { "how do i", "teach me", "help me", "what are the controls", "how to play" },
                BaseWeight = 0.9f
            });

            // Meta patterns
            _patterns.Add(new IntentPattern
            {
                Intent = IntentCategory.Meta,
                Keywords = new[] { "save", "load", "settings", "options", "pause", "menu", "quit", "exit", "restart", "difficulty", "volume", "graphics" },
                Phrases = new[] { "save game", "load game", "open settings", "change options" },
                BaseWeight = 1.0f
            });
        }
    }

    public class EnhancedClassifierConfig
    {
        public float MinConfidenceThreshold { get; set; } = 0.3f;
        public float AmbiguityThreshold { get; set; } = 0.15f;
        public int MaxInputLength { get; set; } = 1000;
        public bool EnableCaching { get; set; } = true;
        public int MaxCacheSize { get; set; } = 1000;
        public int CacheExpirySeconds { get; set; } = 300;
    }
}
