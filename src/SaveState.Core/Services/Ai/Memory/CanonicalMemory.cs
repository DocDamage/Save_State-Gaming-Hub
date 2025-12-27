using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Memory
{
    /// <summary>
    /// Long-term immutable facts - world lore, rules, invariants.
    /// - Relational structure for hard truths
    /// - Version-controlled facts
    /// - Never modified by AI, only by game events
    /// - Source-of-truth for lore validation
    /// </summary>
    public enum FactCategory
    {
        WorldLore,          // History, geography, cosmology
        GameRules,          // Mechanics, systems
        CharacterInfo,      // NPCs, relationships
        ItemLore,           // Artifacts, equipment
        FactionInfo,        // Organizations, politics
        QuestLore,          // Quest backstories
        PlayerHistory,      // Permanent player achievements
        SystemInvariant     // Hard rules that cannot be broken
    }

    public enum FactStatus
    {
        Active,
        Superseded,
        Deprecated,
        Conditional
    }

    public class CanonicalFact
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Statement { get; set; } = string.Empty;
        public FactCategory Category { get; set; }
        public FactStatus Status { get; set; } = FactStatus.Active;
        public int Version { get; set; } = 1;
        public string? SupersededById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public string Source { get; set; } = "system";
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, string> Relations { get; set; } = new();
        public string? Condition { get; set; } // For conditional facts
        public float Confidence { get; set; } = 1.0f;

        // References to related facts
        public List<string> RelatedFactIds { get; set; } = new();
        
        // Contradiction tracking
        public List<string> ConflictsWith { get; set; } = new();
    }

    public class CanonicalMemoryConfig
    {
        public string StoragePath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveState", "Memory", "canonical");
        public bool RequireSourceAttribution { get; set; } = true;
        public bool AllowConditionalFacts { get; set; } = true;
        public bool AutoPersist { get; set; } = true;
    }

    public class LoreValidationResult
    {
        public bool IsValid { get; set; }
        public List<CanonicalFact> SupportingFacts { get; set; } = new();
        public List<CanonicalFact> ConflictingFacts { get; set; } = new();
        public string? ConflictReason { get; set; }
    }

    public interface ICanonicalMemory
    {
        Task<CanonicalFact> AddFact(string statement, FactCategory category, string source,
            List<string>? tags = null, Dictionary<string, string>? relations = null);
        Task<CanonicalFact?> UpdateFact(string factId, string newStatement, string source);
        Task<bool> DeprecateFact(string factId, string reason);
        Task<IEnumerable<CanonicalFact>> Query(string searchText, FactCategory? category = null);
        Task<IEnumerable<CanonicalFact>> GetByCategory(FactCategory category);
        Task<IEnumerable<CanonicalFact>> GetByTag(string tag);
        Task<CanonicalFact?> GetById(string id);
        Task<LoreValidationResult> ValidateStatement(string statement);
        Task<string> BuildLoreContext(List<string> relevantTopics, int maxFacts = 20);
        Task SaveAsync();
        Task LoadAsync();
        int Count { get; }
    }

    public class CanonicalMemory : ICanonicalMemory
    {
        private readonly List<CanonicalFact> _facts = new();
        private readonly Dictionary<string, CanonicalFact> _factIndex = new();
        private readonly Dictionary<FactCategory, List<CanonicalFact>> _categoryIndex = new();
        private readonly Dictionary<string, HashSet<string>> _tagIndex = new();
        private readonly CanonicalMemoryConfig _config;
        private bool _loaded = false;

        public int Count => _facts.Count(f => f.Status == FactStatus.Active);

        public CanonicalMemory(CanonicalMemoryConfig? config = null)
        {
            _config = config ?? new CanonicalMemoryConfig();
            Directory.CreateDirectory(_config.StoragePath);
            
            // Initialize category index
            foreach (FactCategory category in Enum.GetValues<FactCategory>())
            {
                _categoryIndex[category] = new List<CanonicalFact>();
            }
        }

        public async Task<CanonicalFact> AddFact(string statement, FactCategory category, string source,
            List<string>? tags = null, Dictionary<string, string>? relations = null)
        {
            if (!_loaded) await LoadAsync();

            var fact = new CanonicalFact
            {
                Statement = statement,
                Category = category,
                Source = source,
                Tags = tags ?? new List<string>(),
                Relations = relations ?? new Dictionary<string, string>()
            };

            // Check for conflicts with existing facts
            var validation = await ValidateStatement(statement);
            if (validation.ConflictingFacts.Any())
            {
                foreach (var conflict in validation.ConflictingFacts)
                {
                    fact.ConflictsWith.Add(conflict.Id);
                    conflict.ConflictsWith.Add(fact.Id);
                }
            }

            AddToIndices(fact);

            if (_config.AutoPersist)
            {
                await SaveAsync();
            }

            return fact;
        }

        public async Task<CanonicalFact?> UpdateFact(string factId, string newStatement, string source)
        {
            if (!_loaded) await LoadAsync();

            if (!_factIndex.TryGetValue(factId, out var oldFact))
            {
                return null;
            }

            // Supersede the old fact
            oldFact.Status = FactStatus.Superseded;
            oldFact.ModifiedAt = DateTime.UtcNow;

            // Create new version
            var newFact = new CanonicalFact
            {
                Statement = newStatement,
                Category = oldFact.Category,
                Source = source,
                Version = oldFact.Version + 1,
                Tags = new List<string>(oldFact.Tags),
                Relations = new Dictionary<string, string>(oldFact.Relations),
                RelatedFactIds = new List<string>(oldFact.RelatedFactIds)
            };

            oldFact.SupersededById = newFact.Id;
            
            AddToIndices(newFact);

            if (_config.AutoPersist)
            {
                await SaveAsync();
            }

            return newFact;
        }

        public async Task<bool> DeprecateFact(string factId, string reason)
        {
            if (!_loaded) await LoadAsync();

            if (!_factIndex.TryGetValue(factId, out var fact))
            {
                return false;
            }

            fact.Status = FactStatus.Deprecated;
            fact.ModifiedAt = DateTime.UtcNow;
            fact.Relations["deprecation_reason"] = reason;

            if (_config.AutoPersist)
            {
                await SaveAsync();
            }

            return true;
        }

        public async Task<IEnumerable<CanonicalFact>> Query(string searchText, FactCategory? category = null)
        {
            if (!_loaded) await LoadAsync();

            var searchLower = searchText.ToLowerInvariant();
            var terms = searchLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return _facts
                .Where(f => f.Status == FactStatus.Active || f.Status == FactStatus.Conditional)
                .Where(f => !category.HasValue || f.Category == category.Value)
                .Select(f => new
                {
                    Fact = f,
                    Score = CalculateRelevance(f, terms)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Fact.Confidence)
                .Select(x => x.Fact);
        }

        public async Task<IEnumerable<CanonicalFact>> GetByCategory(FactCategory category)
        {
            if (!_loaded) await LoadAsync();

            return _categoryIndex[category]
                .Where(f => f.Status == FactStatus.Active)
                .OrderByDescending(f => f.Confidence);
        }

        public async Task<IEnumerable<CanonicalFact>> GetByTag(string tag)
        {
            if (!_loaded) await LoadAsync();

            if (!_tagIndex.TryGetValue(tag.ToLowerInvariant(), out var factIds))
            {
                return Enumerable.Empty<CanonicalFact>();
            }

            return factIds
                .Select(id => _factIndex.TryGetValue(id, out var f) ? f : null)
                .Where(f => f != null && f.Status == FactStatus.Active)
                .Cast<CanonicalFact>();
        }

        public async Task<CanonicalFact?> GetById(string id)
        {
            if (!_loaded) await LoadAsync();

            return _factIndex.TryGetValue(id, out var fact) ? fact : null;
        }

        public async Task<LoreValidationResult> ValidateStatement(string statement)
        {
            if (!_loaded) await LoadAsync();

            var result = new LoreValidationResult { IsValid = true };
            var statementLower = statement.ToLowerInvariant();

            foreach (var fact in _facts.Where(f => f.Status == FactStatus.Active))
            {
                var factLower = fact.Statement.ToLowerInvariant();

                // Check for contradictions using simple heuristics
                if (DetectContradiction(statementLower, factLower))
                {
                    result.IsValid = false;
                    result.ConflictingFacts.Add(fact);
                    result.ConflictReason = $"Conflicts with: \"{fact.Statement}\"";
                }
                else if (DetectSupport(statementLower, factLower))
                {
                    result.SupportingFacts.Add(fact);
                }
            }

            return result;
        }

        public async Task<string> BuildLoreContext(List<string> relevantTopics, int maxFacts = 20)
        {
            if (!_loaded) await LoadAsync();

            var relevantFacts = new HashSet<CanonicalFact>();

            foreach (var topic in relevantTopics)
            {
                var facts = await Query(topic);
                foreach (var fact in facts.Take(5))
                {
                    relevantFacts.Add(fact);
                }
            }

            var contextBuilder = new System.Text.StringBuilder();
            contextBuilder.AppendLine("=== Canonical Lore (Source of Truth) ===");

            var groupedFacts = relevantFacts
                .Take(maxFacts)
                .GroupBy(f => f.Category);

            foreach (var group in groupedFacts)
            {
                contextBuilder.AppendLine($"\n[{group.Key}]");
                foreach (var fact in group)
                {
                    contextBuilder.AppendLine($"• {fact.Statement}");
                }
            }

            contextBuilder.AppendLine("\n=== End Lore ===");
            contextBuilder.AppendLine("IMPORTANT: The above facts are immutable truths. Never contradict them.");

            return contextBuilder.ToString();
        }

        public async Task SaveAsync()
        {
            var filePath = Path.Combine(_config.StoragePath, "canonical_facts.json");
            var json = JsonSerializer.Serialize(_facts, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task LoadAsync()
        {
            if (_loaded) return;

            var filePath = Path.Combine(_config.StoragePath, "canonical_facts.json");
            if (File.Exists(filePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var facts = JsonSerializer.Deserialize<List<CanonicalFact>>(json);
                    if (facts != null)
                    {
                        _facts.Clear();
                        _factIndex.Clear();
                        foreach (var category in _categoryIndex.Values)
                        {
                            category.Clear();
                        }
                        _tagIndex.Clear();

                        foreach (var fact in facts)
                        {
                            AddToIndices(fact);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading canonical memory: {ex.Message}");
                }
            }

            _loaded = true;
        }

        private void AddToIndices(CanonicalFact fact)
        {
            _facts.Add(fact);
            _factIndex[fact.Id] = fact;
            _categoryIndex[fact.Category].Add(fact);

            foreach (var tag in fact.Tags)
            {
                var tagLower = tag.ToLowerInvariant();
                if (!_tagIndex.ContainsKey(tagLower))
                {
                    _tagIndex[tagLower] = new HashSet<string>();
                }
                _tagIndex[tagLower].Add(fact.Id);
            }
        }

        private float CalculateRelevance(CanonicalFact fact, string[] terms)
        {
            var content = $"{fact.Statement} {string.Join(" ", fact.Tags)}".ToLowerInvariant();
            float score = 0;

            foreach (var term in terms)
            {
                if (content.Contains(term))
                {
                    score += 1.0f;
                    // Bonus for exact word match
                    if (content.Split(' ').Contains(term))
                    {
                        score += 0.5f;
                    }
                }
            }

            return score * fact.Confidence;
        }

        private bool DetectContradiction(string statement1, string statement2)
        {
            // Simple contradiction detection heuristics
            var negationPatterns = new[] { "not ", "never ", "no ", "cannot ", "isn't ", "doesn't ", "won't " };

            foreach (var pattern in negationPatterns)
            {
                // If one has negation and they share key terms, might be a contradiction
                var s1HasNegation = statement1.Contains(pattern);
                var s2HasNegation = statement2.Contains(pattern);

                if (s1HasNegation != s2HasNegation)
                {
                    // Check if they share significant terms
                    var terms1 = statement1.Replace(pattern, "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var terms2 = statement2.Replace(pattern, "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var commonTerms = terms1.Intersect(terms2).Where(t => t.Length > 3).Count();

                    if (commonTerms >= 3)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool DetectSupport(string statement1, string statement2)
        {
            var terms1 = statement1.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 3).ToHashSet();
            var terms2 = statement2.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 3).ToHashSet();

            var overlap = terms1.Intersect(terms2).Count();
            return overlap >= 3;
        }

        /// <summary>
        /// Initialize with default game lore
        /// </summary>
        public async Task SeedDefaultLore(string gameId)
        {
            if (!_loaded) await LoadAsync();

            // Add some default invariant rules
            var invariants = new[]
            {
                "The player's decisions shape the game world.",
                "NPCs remember player actions and react accordingly.",
                "Completed quests cannot be undone.",
                "Player achievements are permanent."
            };

            foreach (var invariant in invariants)
            {
                if (!_facts.Any(f => f.Statement == invariant))
                {
                    await AddFact(invariant, FactCategory.SystemInvariant, "system",
                        new List<string> { "core", "invariant", gameId });
                }
            }
        }
    }
}
