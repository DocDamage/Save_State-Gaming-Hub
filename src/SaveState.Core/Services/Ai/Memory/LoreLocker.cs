using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Memory
{
    /// <summary>
    /// Enhanced canon management with lore locking, confidence scoring,
    /// and rejection/regeneration pipeline.
    /// 
    /// Key principle: RAG helps retrieve lore. This enforces canon.
    /// </summary>
    public interface ILoreLocker
    {
        /// <summary>
        /// Validate a statement against locked lore
        /// </summary>
        Task<LoreLockerValidationResult> ValidateAsync(string statement, LoreContext? context = null);

        /// <summary>
        /// Lock a piece of lore as canon
        /// </summary>
        void LockLore(LockedLore lore);

        /// <summary>
        /// Request lore modification (goes through approval pipeline)
        /// </summary>
        Task<LoreModificationResult> RequestModificationAsync(LoreModificationRequest request);

        /// <summary>
        /// Get confidence score for a statement
        /// </summary>
        Task<LoreConfidenceScore> GetConfidenceAsync(string statement);

        /// <summary>
        /// Regenerate content that violated lore
        /// </summary>
        Task<string> RegenerateCompliantAsync(string violatingContent, List<LoreViolation> violations);

        /// <summary>
        /// Get all locked lore for a category
        /// </summary>
        IEnumerable<LockedLore> GetLockedLore(string? category = null);
    }

    /// <summary>
    /// A piece of locked lore
    /// </summary>
    public class LockedLore
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Statement { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public LoreLockType LockType { get; set; } = LoreLockType.Immutable;
        public double ConfidenceThreshold { get; set; } = 0.8;
        public string Source { get; set; } = string.Empty;
        public DateTime LockedAt { get; set; } = DateTime.UtcNow;
        public List<string> Keywords { get; set; } = new();
        public List<string> RelatedLoreIds { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of lore locks
    /// </summary>
    public enum LoreLockType
    {
        /// <summary>Cannot be changed under any circumstances</summary>
        Immutable,
        
        /// <summary>Can only be changed by story events with proper justification</summary>
        EventMutable,
        
        /// <summary>Can have variations but core truth must be maintained</summary>
        VariantAllowed,
        
        /// <summary>Soft lock - can be evolved but changes are logged</summary>
        Evolving
    }

    /// <summary>
    /// Context for lore validation
    /// </summary>
    public class LoreContext
    {
        public string? CurrentChapter { get; set; }
        public string? CurrentLocation { get; set; }
        public List<string> ActiveQuests { get; set; } = new();
        public List<string> KnownCharacters { get; set; } = new();
        public DateTime? GameTime { get; set; }
    }

    /// <summary>
    /// Result of lore validation
    /// </summary>
    public class LoreLockerValidationResult
    {
        public bool IsCompliant { get; set; }
        public List<LoreViolation> Violations { get; set; } = new();
        public double OverallConfidence { get; set; } = 1.0;
        public List<LockedLore> RelatedLore { get; set; } = new();
        public string? SuggestedRevision { get; set; }

        public static LoreLockerValidationResult Compliant(double confidence = 1.0)
            => new() { IsCompliant = true, OverallConfidence = confidence };

        public static LoreLockerValidationResult NonCompliant(List<LoreViolation> violations)
            => new() { IsCompliant = false, Violations = violations };
    }

    /// <summary>
    /// A specific lore violation
    /// </summary>
    public class LoreViolation
    {
        public LockedLore ViolatedLore { get; set; } = new();
        public string ViolationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public LoreViolationSeverity Severity { get; set; }
        public string? CorrectStatement { get; set; }
    }

    /// <summary>
    /// Severity of lore violation
    /// </summary>
    public enum LoreViolationSeverity
    {
        Minor,          // Small inconsistency
        Moderate,       // Noticeable but not breaking
        Major,          // Significant lore break
        Critical,       // Fundamental contradiction
        Resurrection    // A dead character referenced as alive
    }

    /// <summary>
    /// Confidence score for lore
    /// </summary>
    public class LoreConfidenceScore
    {
        public double Score { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public List<LockedLore> SupportingLore { get; set; } = new();
        public List<LockedLore> ConflictingLore { get; set; } = new();
    }

    /// <summary>
    /// Request to modify lore
    /// </summary>
    public class LoreModificationRequest
    {
        public string TargetLoreId { get; set; } = string.Empty;
        public string ProposedChange { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public bool IsStoryEvent { get; set; }
    }

    /// <summary>
    /// Result of lore modification request
    /// </summary>
    public class LoreModificationResult
    {
        public bool Approved { get; set; }
        public string? RejectionReason { get; set; }
        public LockedLore? ModifiedLore { get; set; }
    }

    /// <summary>
    /// Default implementation of lore locker
    /// </summary>
    public class LoreLocker : ILoreLocker
    {
        private readonly ConcurrentDictionary<string, LockedLore> _lockedLore = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _categoryIndex = new();
        private readonly Func<string, Task<string>>? _regenerator;

        public LoreLocker(Func<string, Task<string>>? regenerator = null)
        {
            _regenerator = regenerator;
            RegisterDefaultLore();
        }

        public async Task<LoreLockerValidationResult> ValidateAsync(string statement, LoreContext? context = null)
        {
            var violations = new List<LoreViolation>();
            var relatedLore = new List<LockedLore>();
            var statementLower = statement.ToLowerInvariant();
            double minConfidence = 1.0;

            foreach (var lore in _lockedLore.Values)
            {
                var loreLower = lore.Statement.ToLowerInvariant();

                // Check for keyword matches
                var keywordMatch = lore.Keywords.Any(k => 
                    statementLower.Contains(k.ToLowerInvariant()));

                if (keywordMatch || IsRelated(statementLower, loreLower))
                {
                    relatedLore.Add(lore);

                    // Check for contradictions
                    var contradiction = DetectContradiction(statement, lore);
                    if (contradiction != null)
                    {
                        violations.Add(contradiction);
                        minConfidence = Math.Min(minConfidence, 1.0 - (int)contradiction.Severity * 0.25);
                    }

                    // Check for resurrection (dead character as alive)
                    if (lore.Category == "deceased" && ContainsAliveReference(statementLower, lore))
                    {
                        violations.Add(new LoreViolation
                        {
                            ViolatedLore = lore,
                            ViolationType = "Resurrection",
                            Description = $"Referenced deceased character as alive: {lore.Statement}",
                            Severity = LoreViolationSeverity.Resurrection,
                            CorrectStatement = $"This character is deceased: {lore.Statement}"
                        });
                        minConfidence = 0;
                    }
                }
            }

            if (violations.Any())
            {
                var result = LoreLockerValidationResult.NonCompliant(violations);
                result.RelatedLore = relatedLore;
                result.OverallConfidence = minConfidence;
                result.SuggestedRevision = await GenerateSuggestedRevision(statement, violations);
                return result;
            }

            return new LoreLockerValidationResult
            {
                IsCompliant = true,
                OverallConfidence = minConfidence,
                RelatedLore = relatedLore
            };
        }

        public void LockLore(LockedLore lore)
        {
            _lockedLore[lore.Id] = lore;

            if (!_categoryIndex.ContainsKey(lore.Category))
            {
                _categoryIndex[lore.Category] = new HashSet<string>();
            }
            _categoryIndex[lore.Category].Add(lore.Id);
        }

        public async Task<LoreModificationResult> RequestModificationAsync(LoreModificationRequest request)
        {
            if (!_lockedLore.TryGetValue(request.TargetLoreId, out var lore))
            {
                return new LoreModificationResult
                {
                    Approved = false,
                    RejectionReason = "Lore not found"
                };
            }

            switch (lore.LockType)
            {
                case LoreLockType.Immutable:
                    return new LoreModificationResult
                    {
                        Approved = false,
                        RejectionReason = "This lore is immutable and cannot be changed"
                    };

                case LoreLockType.EventMutable:
                    if (!request.IsStoryEvent)
                    {
                        return new LoreModificationResult
                        {
                            Approved = false,
                            RejectionReason = "This lore can only be changed by story events"
                        };
                    }
                    break;
            }

            // Apply modification
            var modified = new LockedLore
            {
                Id = lore.Id,
                Statement = request.ProposedChange,
                Category = lore.Category,
                LockType = lore.LockType,
                Source = $"Modified: {request.Justification}",
                Keywords = lore.Keywords,
                Metadata = new Dictionary<string, object>(lore.Metadata)
                {
                    ["previous_statement"] = lore.Statement,
                    ["modified_by"] = request.RequestedBy,
                    ["modified_at"] = DateTime.UtcNow
                }
            };

            LockLore(modified);

            return new LoreModificationResult
            {
                Approved = true,
                ModifiedLore = modified
            };
        }

        public Task<LoreConfidenceScore> GetConfidenceAsync(string statement)
        {
            var supporting = new List<LockedLore>();
            var conflicting = new List<LockedLore>();
            var statementLower = statement.ToLowerInvariant();

            foreach (var lore in _lockedLore.Values)
            {
                if (IsRelated(statementLower, lore.Statement.ToLowerInvariant()))
                {
                    if (DetectContradiction(statement, lore) != null)
                    {
                        conflicting.Add(lore);
                    }
                    else
                    {
                        supporting.Add(lore);
                    }
                }
            }

            var score = 1.0;
            score -= conflicting.Count * 0.3;
            score += supporting.Count * 0.1;
            score = Math.Clamp(score, 0, 1);

            return Task.FromResult(new LoreConfidenceScore
            {
                Score = score,
                Reasoning = conflicting.Any()
                    ? $"Conflicts with {conflicting.Count} locked lore entries"
                    : $"Consistent with {supporting.Count} related lore entries",
                SupportingLore = supporting,
                ConflictingLore = conflicting
            });
        }

        public async Task<string> RegenerateCompliantAsync(string violatingContent, List<LoreViolation> violations)
        {
            if (_regenerator == null)
            {
                return GenerateManualCorrection(violatingContent, violations);
            }

            var constraints = violations.Select(v => v.CorrectStatement ?? v.ViolatedLore.Statement);
            var prompt = $"Revise the following content to be consistent with these established facts:\n" +
                        $"Facts: {string.Join("; ", constraints)}\n\n" +
                        $"Original content: {violatingContent}\n\n" +
                        $"Revised content (maintaining the same intent but correcting lore violations):";

            return await _regenerator(prompt);
        }

        public IEnumerable<LockedLore> GetLockedLore(string? category = null)
        {
            if (category == null)
            {
                return _lockedLore.Values;
            }

            if (_categoryIndex.TryGetValue(category, out var ids))
            {
                return ids.Select(id => _lockedLore.TryGetValue(id, out var lore) ? lore : null)
                    .Where(l => l != null)
                    .Cast<LockedLore>();
            }

            return Enumerable.Empty<LockedLore>();
        }

        private bool IsRelated(string statement, string lore)
        {
            var statementWords = statement.Split(' ').Where(w => w.Length > 3).ToHashSet();
            var loreWords = lore.Split(' ').Where(w => w.Length > 3).ToHashSet();
            return statementWords.Intersect(loreWords, StringComparer.OrdinalIgnoreCase).Count() >= 2;
        }

        private LoreViolation? DetectContradiction(string statement, LockedLore lore)
        {
            var statementLower = statement.ToLowerInvariant();
            var loreLower = lore.Statement.ToLowerInvariant();

            var contradictionPatterns = new[]
            {
                ("is alive", "is dead"),
                ("is dead", "is alive"),
                ("exists", "doesn't exist"),
                ("never happened", "occurred"),
                ("was destroyed", "still stands"),
                ("founded", "never existed")
            };

            foreach (var (pattern1, pattern2) in contradictionPatterns)
            {
                if ((statementLower.Contains(pattern1) && loreLower.Contains(pattern2)) ||
                    (statementLower.Contains(pattern2) && loreLower.Contains(pattern1)))
                {
                    return new LoreViolation
                    {
                        ViolatedLore = lore,
                        ViolationType = "DirectContradiction",
                        Description = $"Statement contradicts: {lore.Statement}",
                        Severity = lore.LockType == LoreLockType.Immutable 
                            ? LoreViolationSeverity.Critical 
                            : LoreViolationSeverity.Major,
                        CorrectStatement = lore.Statement
                    };
                }
            }

            return null;
        }

        private bool ContainsAliveReference(string statement, LockedLore deceasedLore)
        {
            var aliveIndicators = new[] { "says", "tells", "speaks", "greets", "is here", "arrives" };
            var nameWords = deceasedLore.Keywords.Any() 
                ? deceasedLore.Keywords 
                : deceasedLore.Statement.Split(' ').Where(w => char.IsUpper(w[0])).ToList();

            foreach (var name in nameWords)
            {
                if (statement.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (aliveIndicators.Any(ind => statement.Contains(ind, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private async Task<string?> GenerateSuggestedRevision(string statement, List<LoreViolation> violations)
        {
            if (!violations.Any()) return null;

            var corrections = violations
                .Where(v => v.CorrectStatement != null)
                .Select(v => v.CorrectStatement)
                .ToList();

            if (corrections.Any())
            {
                return $"Consider revising to align with: {string.Join("; ", corrections)}";
            }

            return "Please revise to align with established lore.";
        }

        private string GenerateManualCorrection(string content, List<LoreViolation> violations)
        {
            var correction = content;
            foreach (var violation in violations.Where(v => v.Severity >= LoreViolationSeverity.Major))
            {
                correction = $"[LORE VIOLATION: {violation.Description}] " + correction;
            }
            return correction;
        }

        private void RegisterDefaultLore()
        {
            // Example locked lore - would be loaded from game data
            LockLore(new LockedLore
            {
                Statement = "The First King founded the realm 500 years ago",
                Category = "history",
                LockType = LoreLockType.Immutable,
                Keywords = new List<string> { "First King", "founded", "realm" },
                Source = "core_lore"
            });

            LockLore(new LockedLore
            {
                Statement = "Lord Aldric died during the Siege of Thornwall",
                Category = "deceased",
                LockType = LoreLockType.Immutable,
                Keywords = new List<string> { "Aldric", "Thornwall" },
                Source = "main_story"
            });

            LockLore(new LockedLore
            {
                Statement = "Magic requires a connection to the Aether",
                Category = "magic_system",
                LockType = LoreLockType.Immutable,
                Keywords = new List<string> { "magic", "Aether" },
                Source = "world_rules"
            });
        }
    }
}
