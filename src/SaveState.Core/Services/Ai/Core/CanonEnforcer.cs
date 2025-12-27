using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Core
{
    /// <summary>
    /// Enforces canonical state integrity.
    /// Canon is the source of truth for the game world.
    /// </summary>
    public interface ICanonEnforcer
    {
        /// <summary>
        /// Validate a statement against canon
        /// </summary>
        Task<CanonValidationResult> ValidateAsync(string statement, CanonContext context);

        /// <summary>
        /// Register a canonical fact
        /// </summary>
        void RegisterFact(CanonicalFact fact);

        /// <summary>
        /// Check if a specific claim contradicts canon
        /// </summary>
        Task<bool> ContradictsCanonicAsync(string claim, string category);

        /// <summary>
        /// Get canon checksum for drift detection
        /// </summary>
        string GetCanonChecksum();

        /// <summary>
        /// Get all facts in a category
        /// </summary>
        IEnumerable<CanonicalFact> GetFacts(string? category = null);
    }

    /// <summary>
    /// A canonical fact in the game world
    /// </summary>
    public class CanonicalFact
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Statement { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public CanonMutability Mutability { get; set; } = CanonMutability.Immutable;
        public double Confidence { get; set; } = 1.0;
        public string Source { get; set; } = string.Empty;
        public DateTime EstablishedAt { get; set; } = DateTime.UtcNow;
        public List<string> Tags { get; set; } = new();
        public List<string> RelatedFactIds { get; set; } = new();
    }

    /// <summary>
    /// Whether a canonical fact can be changed
    /// </summary>
    public enum CanonMutability
    {
        /// <summary>Cannot ever be changed</summary>
        Immutable,
        
        /// <summary>Can be changed by specific story events</summary>
        EventMutable,
        
        /// <summary>Can evolve over time within constraints</summary>
        Evolvable,
        
        /// <summary>Derived from other facts, updates automatically</summary>
        Derived
    }

    /// <summary>
    /// Context for canon validation
    /// </summary>
    public class CanonContext
    {
        public string? CurrentLocation { get; set; }
        public string? CurrentQuest { get; set; }
        public DateTime? GameTime { get; set; }
        public List<string> RelevantCategories { get; set; } = new();
    }

    /// <summary>
    /// Result of canon validation
    /// </summary>
    public class CanonValidationResult
    {
        public bool IsValid { get; set; }
        public List<CanonViolation> Violations { get; set; } = new();
        public List<CanonicalFact> RelatedFacts { get; set; } = new();
        public double OverallConfidence { get; set; } = 1.0;
        public string? SuggestedCorrection { get; set; }

        public static CanonValidationResult Valid(List<CanonicalFact>? related = null)
            => new() { IsValid = true, RelatedFacts = related ?? new() };

        public static CanonValidationResult Invalid(List<CanonViolation> violations, string? correction = null)
            => new() { IsValid = false, Violations = violations, SuggestedCorrection = correction };
    }

    /// <summary>
    /// A violation of canon
    /// </summary>
    public class CanonViolation
    {
        public string ViolationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CanonicalFact? ConflictingFact { get; set; }
        public CanonViolationSeverity Severity { get; set; }
    }

    /// <summary>
    /// How severe a canon violation is
    /// </summary>
    public enum CanonViolationSeverity
    {
        Minor,      // Slight inconsistency
        Moderate,   // Noticeable contradiction
        Major,      // Significant lore break
        Critical    // Fundamental world violation
    }

    /// <summary>
    /// Default implementation of canon enforcer
    /// </summary>
    public class CanonEnforcer : ICanonEnforcer
    {
        private readonly ConcurrentDictionary<string, CanonicalFact> _facts = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _categoryIndex = new();
        private string _cachedChecksum = string.Empty;
        private bool _checksumDirty = true;

        public CanonEnforcer()
        {
            RegisterDefaultFacts();
        }

        public Task<CanonValidationResult> ValidateAsync(string statement, CanonContext context)
        {
            var violations = new List<CanonViolation>();
            var relatedFacts = new List<CanonicalFact>();
            var statementLower = statement.ToLowerInvariant();

            // Find relevant facts based on context
            var relevantFacts = GetRelevantFacts(context);

            foreach (var fact in relevantFacts)
            {
                var factLower = fact.Statement.ToLowerInvariant();

                // Check for direct contradictions
                if (ContainsContradiction(statementLower, factLower))
                {
                    violations.Add(new CanonViolation
                    {
                        ViolationType = "DirectContradiction",
                        Description = $"Statement contradicts established fact: {fact.Statement}",
                        ConflictingFact = fact,
                        Severity = fact.Mutability == CanonMutability.Immutable 
                            ? CanonViolationSeverity.Critical 
                            : CanonViolationSeverity.Major
                    });
                }

                // Check for related content
                if (IsRelated(statementLower, factLower))
                {
                    relatedFacts.Add(fact);
                }
            }

            if (violations.Any())
            {
                var correction = GenerateSuggestedCorrection(statement, violations);
                return Task.FromResult(CanonValidationResult.Invalid(violations, correction));
            }

            return Task.FromResult(CanonValidationResult.Valid(relatedFacts));
        }

        public void RegisterFact(CanonicalFact fact)
        {
            _facts[fact.Id] = fact;

            // Update category index
            if (!_categoryIndex.ContainsKey(fact.Category))
            {
                _categoryIndex[fact.Category] = new HashSet<string>();
            }
            _categoryIndex[fact.Category].Add(fact.Id);

            _checksumDirty = true;
        }

        public async Task<bool> ContradictsCanonicAsync(string claim, string category)
        {
            var context = new CanonContext { RelevantCategories = new List<string> { category } };
            var result = await ValidateAsync(claim, context);
            return !result.IsValid;
        }

        public string GetCanonChecksum()
        {
            if (!_checksumDirty && !string.IsNullOrEmpty(_cachedChecksum))
            {
                return _cachedChecksum;
            }

            var orderedFacts = _facts.Values
                .OrderBy(f => f.Id)
                .Select(f => $"{f.Id}:{f.Statement}:{f.Mutability}")
                .ToList();

            var combined = string.Join("|", orderedFacts);
            
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            _cachedChecksum = Convert.ToBase64String(bytes);
            _checksumDirty = false;

            return _cachedChecksum;
        }

        public IEnumerable<CanonicalFact> GetFacts(string? category = null)
        {
            if (category == null)
            {
                return _facts.Values;
            }

            if (_categoryIndex.TryGetValue(category, out var factIds))
            {
                return factIds
                    .Select(id => _facts.TryGetValue(id, out var fact) ? fact : null)
                    .Where(f => f != null)
                    .Cast<CanonicalFact>();
            }

            return Enumerable.Empty<CanonicalFact>();
        }

        private IEnumerable<CanonicalFact> GetRelevantFacts(CanonContext context)
        {
            if (context.RelevantCategories.Any())
            {
                return context.RelevantCategories
                    .SelectMany(c => GetFacts(c))
                    .Distinct();
            }

            return _facts.Values;
        }

        private bool ContainsContradiction(string statement, string fact)
        {
            // Simplified contradiction detection
            // In production, this would use NLP/embeddings
            
            var contradictionPatterns = new[]
            {
                ("is alive", "is dead"),
                ("is dead", "is alive"),
                ("exists", "doesn't exist"),
                ("never", "always"),
                ("was destroyed", "still stands"),
                ("won the war", "lost the war"),
                ("is good", "is evil"),
                ("is evil", "is good")
            };

            foreach (var (pattern1, pattern2) in contradictionPatterns)
            {
                if ((statement.Contains(pattern1) && fact.Contains(pattern2)) ||
                    (statement.Contains(pattern2) && fact.Contains(pattern1)))
                {
                    // Check for same subject (simplified)
                    var statementWords = statement.Split(' ').Take(5);
                    var factWords = fact.Split(' ').Take(5);
                    
                    if (statementWords.Intersect(factWords, StringComparer.OrdinalIgnoreCase).Any())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsRelated(string statement, string fact)
        {
            var statementWords = statement.Split(' ')
                .Where(w => w.Length > 3)
                .Select(w => w.ToLowerInvariant())
                .ToHashSet();

            var factWords = fact.Split(' ')
                .Where(w => w.Length > 3)
                .Select(w => w.ToLowerInvariant())
                .ToHashSet();

            var overlap = statementWords.Intersect(factWords).Count();
            return overlap >= 2;
        }

        private string? GenerateSuggestedCorrection(string statement, List<CanonViolation> violations)
        {
            if (!violations.Any()) return null;

            var primary = violations.OrderByDescending(v => v.Severity).First();
            
            if (primary.ConflictingFact != null)
            {
                return $"Consider revising to align with: {primary.ConflictingFact.Statement}";
            }

            return "Please revise the statement to align with established canon.";
        }

        private void RegisterDefaultFacts()
        {
            // Example canonical facts - would be loaded from game data
            RegisterFact(new CanonicalFact
            {
                Statement = "The Great Cataclysm occurred 1000 years ago",
                Category = "history",
                Mutability = CanonMutability.Immutable,
                Source = "core_lore"
            });

            RegisterFact(new CanonicalFact
            {
                Statement = "The five kingdoms were united after the Cataclysm",
                Category = "history",
                Mutability = CanonMutability.Immutable,
                Source = "core_lore"
            });

            RegisterFact(new CanonicalFact
            {
                Statement = "Magic flows from the Aether dimension",
                Category = "magic_system",
                Mutability = CanonMutability.Immutable,
                Source = "core_lore"
            });

            RegisterFact(new CanonicalFact
            {
                Statement = "The Dragon King was slain by the First Hero",
                Category = "mythology",
                Mutability = CanonMutability.Immutable,
                Source = "core_lore"
            });
        }
    }
}
