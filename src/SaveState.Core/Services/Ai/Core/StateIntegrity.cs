using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Core
{
    /// <summary>
    /// Maintains state integrity to prevent save corruption, balance collapse, and lore rot.
    /// Provides validation, checksums, and rollback capabilities for critical game state.
    /// </summary>
    public interface IStateIntegrity
    {
        /// <summary>
        /// Create a snapshot of current state
        /// </summary>
        StateSnapshot CreateSnapshot(string snapshotId, Dictionary<string, object> state);

        /// <summary>
        /// Validate state against integrity rules
        /// </summary>
        IntegrityCheckResult ValidateState(Dictionary<string, object> state);

        /// <summary>
        /// Check if state has drifted from snapshot
        /// </summary>
        DriftCheckResult CheckDrift(string snapshotId, Dictionary<string, object> currentState);

        /// <summary>
        /// Get a previous snapshot for rollback
        /// </summary>
        StateSnapshot? GetSnapshot(string snapshotId);

        /// <summary>
        /// Register an integrity rule
        /// </summary>
        void RegisterRule(IntegrityRule rule);

        /// <summary>
        /// Get all integrity violations
        /// </summary>
        IEnumerable<IntegrityViolation> GetViolations(string? sessionId = null);
    }

    /// <summary>
    /// A snapshot of game state at a point in time
    /// </summary>
    public class StateSnapshot
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Checksum { get; set; } = string.Empty;
        public Dictionary<string, object> State { get; set; } = new();
        public Dictionary<string, string> PathChecksums { get; set; } = new();
        public string? ParentSnapshotId { get; set; }
    }

    /// <summary>
    /// Result of state integrity check
    /// </summary>
    public class IntegrityCheckResult
    {
        public bool IsValid { get; set; }
        public List<IntegrityViolation> Violations { get; set; } = new();
        public Dictionary<string, object> CorrectedValues { get; set; } = new();

        public static IntegrityCheckResult Valid() => new() { IsValid = true };
        
        public static IntegrityCheckResult Invalid(List<IntegrityViolation> violations)
            => new() { IsValid = false, Violations = violations };
    }

    /// <summary>
    /// Result of drift check
    /// </summary>
    public class DriftCheckResult
    {
        public bool HasDrift { get; set; }
        public List<StateDrift> Drifts { get; set; } = new();
        public double DriftPercentage { get; set; }
    }

    /// <summary>
    /// A specific drift in state
    /// </summary>
    public class StateDrift
    {
        public string Path { get; set; } = string.Empty;
        public object? OriginalValue { get; set; }
        public object? CurrentValue { get; set; }
        public DriftSeverity Severity { get; set; }
    }

    /// <summary>
    /// Severity of state drift
    /// </summary>
    public enum DriftSeverity
    {
        Cosmetic,   // Visual only, no gameplay impact
        Minor,      // Small gameplay impact
        Moderate,   // Noticeable impact
        Major,      // Significant game state corruption
        Critical    // Save may be corrupted
    }

    /// <summary>
    /// An integrity violation
    /// </summary>
    public class IntegrityViolation
    {
        public string RuleId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object? ActualValue { get; set; }
        public object? ExpectedValue { get; set; }
        public bool AutoCorrectable { get; set; }
    }

    /// <summary>
    /// A rule for state integrity
    /// </summary>
    public class IntegrityRule
    {
        public string RuleId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IntegrityRuleType Type { get; set; }
        public string Path { get; set; } = string.Empty;
        
        /// <summary>
        /// For range checks
        /// </summary>
        public double? MinValue { get; set; }
        public double? MaxValue { get; set; }
        
        /// <summary>
        /// For type checks
        /// </summary>
        public Type? ExpectedType { get; set; }
        
        /// <summary>
        /// For reference checks - paths that must exist
        /// </summary>
        public List<string> RequiredReferences { get; set; } = new();
        
        /// <summary>
        /// Custom validation function
        /// </summary>
        public Func<object?, bool>? CustomValidator { get; set; }
        
        /// <summary>
        /// Auto-correction function
        /// </summary>
        public Func<object?, object?>? AutoCorrect { get; set; }
    }

    /// <summary>
    /// Types of integrity rules
    /// </summary>
    public enum IntegrityRuleType
    {
        Required,       // Value must exist
        Range,          // Numeric value must be in range
        TypeCheck,      // Value must be of expected type
        Reference,      // Must reference existing entities
        Custom,         // Custom validation logic
        Relationship    // Relationship between values
    }

    /// <summary>
    /// Default implementation of state integrity
    /// </summary>
    public class StateIntegrity : IStateIntegrity
    {
        private readonly ConcurrentDictionary<string, StateSnapshot> _snapshots = new();
        private readonly ConcurrentDictionary<string, IntegrityRule> _rules = new();
        private readonly ConcurrentDictionary<string, List<IntegrityViolation>> _violations = new();

        public StateIntegrity()
        {
            RegisterDefaultRules();
        }

        public StateSnapshot CreateSnapshot(string snapshotId, Dictionary<string, object> state)
        {
            var snapshot = new StateSnapshot
            {
                Id = snapshotId,
                State = new Dictionary<string, object>(state),
                Checksum = ComputeChecksum(state),
                PathChecksums = ComputePathChecksums(state)
            };

            // Link to previous if exists
            var lastSnapshotId = _snapshots.Keys
                .OrderByDescending(k => _snapshots[k].CreatedAt)
                .FirstOrDefault();
            
            if (lastSnapshotId != null)
            {
                snapshot.ParentSnapshotId = lastSnapshotId;
            }

            _snapshots[snapshotId] = snapshot;
            return snapshot;
        }

        public IntegrityCheckResult ValidateState(Dictionary<string, object> state)
        {
            var violations = new List<IntegrityViolation>();
            var corrections = new Dictionary<string, object>();

            foreach (var rule in _rules.Values)
            {
                var value = GetValueAtPath(state, rule.Path);
                var violation = CheckRule(rule, value);

                if (violation != null)
                {
                    violations.Add(violation);

                    if (violation.AutoCorrectable && rule.AutoCorrect != null)
                    {
                        corrections[rule.Path] = rule.AutoCorrect(value)!;
                    }
                }
            }

            if (violations.Any())
            {
                return new IntegrityCheckResult
                {
                    IsValid = false,
                    Violations = violations,
                    CorrectedValues = corrections
                };
            }

            return IntegrityCheckResult.Valid();
        }

        public DriftCheckResult CheckDrift(string snapshotId, Dictionary<string, object> currentState)
        {
            if (!_snapshots.TryGetValue(snapshotId, out var snapshot))
            {
                return new DriftCheckResult { HasDrift = false };
            }

            var drifts = new List<StateDrift>();
            var currentChecksums = ComputePathChecksums(currentState);

            foreach (var (path, originalChecksum) in snapshot.PathChecksums)
            {
                if (!currentChecksums.TryGetValue(path, out var currentChecksum) ||
                    originalChecksum != currentChecksum)
                {
                    var originalValue = GetValueAtPath(snapshot.State, path);
                    var currentValue = GetValueAtPath(currentState, path);

                    drifts.Add(new StateDrift
                    {
                        Path = path,
                        OriginalValue = originalValue,
                        CurrentValue = currentValue,
                        Severity = DetermineDriftSeverity(path)
                    });
                }
            }

            var totalPaths = snapshot.PathChecksums.Count;
            var driftPercentage = totalPaths > 0 ? (double)drifts.Count / totalPaths * 100 : 0;

            return new DriftCheckResult
            {
                HasDrift = drifts.Any(),
                Drifts = drifts,
                DriftPercentage = driftPercentage
            };
        }

        public StateSnapshot? GetSnapshot(string snapshotId)
        {
            return _snapshots.TryGetValue(snapshotId, out var snapshot) ? snapshot : null;
        }

        public void RegisterRule(IntegrityRule rule)
        {
            _rules[rule.RuleId] = rule;
        }

        public IEnumerable<IntegrityViolation> GetViolations(string? sessionId = null)
        {
            if (sessionId == null)
            {
                return _violations.Values.SelectMany(v => v);
            }
            return _violations.TryGetValue(sessionId, out var violations)
                ? violations
                : Enumerable.Empty<IntegrityViolation>();
        }

        private IntegrityViolation? CheckRule(IntegrityRule rule, object? value)
        {
            switch (rule.Type)
            {
                case IntegrityRuleType.Required:
                    if (value == null)
                    {
                        return new IntegrityViolation
                        {
                            RuleId = rule.RuleId,
                            Path = rule.Path,
                            Description = $"Required value missing at {rule.Path}",
                            ActualValue = null,
                            ExpectedValue = "non-null value"
                        };
                    }
                    break;

                case IntegrityRuleType.Range:
                    if (value is double numValue)
                    {
                        if (rule.MinValue.HasValue && numValue < rule.MinValue.Value)
                        {
                            return new IntegrityViolation
                            {
                                RuleId = rule.RuleId,
                                Path = rule.Path,
                                Description = $"Value {numValue} below minimum {rule.MinValue}",
                                ActualValue = numValue,
                                ExpectedValue = $">= {rule.MinValue}",
                                AutoCorrectable = rule.AutoCorrect != null
                            };
                        }
                        if (rule.MaxValue.HasValue && numValue > rule.MaxValue.Value)
                        {
                            return new IntegrityViolation
                            {
                                RuleId = rule.RuleId,
                                Path = rule.Path,
                                Description = $"Value {numValue} above maximum {rule.MaxValue}",
                                ActualValue = numValue,
                                ExpectedValue = $"<= {rule.MaxValue}",
                                AutoCorrectable = rule.AutoCorrect != null
                            };
                        }
                    }
                    break;

                case IntegrityRuleType.TypeCheck:
                    if (value != null && rule.ExpectedType != null &&
                        !rule.ExpectedType.IsInstanceOfType(value))
                    {
                        return new IntegrityViolation
                        {
                            RuleId = rule.RuleId,
                            Path = rule.Path,
                            Description = $"Type mismatch at {rule.Path}",
                            ActualValue = value.GetType().Name,
                            ExpectedValue = rule.ExpectedType.Name
                        };
                    }
                    break;

                case IntegrityRuleType.Custom:
                    if (rule.CustomValidator != null && !rule.CustomValidator(value))
                    {
                        return new IntegrityViolation
                        {
                            RuleId = rule.RuleId,
                            Path = rule.Path,
                            Description = rule.Description,
                            ActualValue = value,
                            AutoCorrectable = rule.AutoCorrect != null
                        };
                    }
                    break;
            }

            return null;
        }

        private object? GetValueAtPath(Dictionary<string, object> state, string path)
        {
            var parts = path.Split('.');
            object? current = state;

            foreach (var part in parts)
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (!dict.TryGetValue(part, out current))
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        private string ComputeChecksum(Dictionary<string, object> state)
        {
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false });
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(bytes);
        }

        private Dictionary<string, string> ComputePathChecksums(Dictionary<string, object> state, string prefix = "")
        {
            var checksums = new Dictionary<string, string>();

            foreach (var (key, value) in state)
            {
                var path = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";

                if (value is Dictionary<string, object> nested)
                {
                    var nestedChecksums = ComputePathChecksums(nested, path);
                    foreach (var (nestedPath, checksum) in nestedChecksums)
                    {
                        checksums[nestedPath] = checksum;
                    }
                }
                else
                {
                    var valueJson = JsonSerializer.Serialize(value);
                    using var sha256 = SHA256.Create();
                    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(valueJson));
                    checksums[path] = Convert.ToBase64String(bytes);
                }
            }

            return checksums;
        }

        private DriftSeverity DetermineDriftSeverity(string path)
        {
            if (path.StartsWith("economy.") || path.StartsWith("progression."))
                return DriftSeverity.Critical;
            if (path.StartsWith("combat.") || path.StartsWith("inventory."))
                return DriftSeverity.Major;
            if (path.StartsWith("quests.") || path.StartsWith("relationships."))
                return DriftSeverity.Moderate;
            if (path.StartsWith("npc.mood") || path.StartsWith("dialogue."))
                return DriftSeverity.Minor;
            return DriftSeverity.Cosmetic;
        }

        private void RegisterDefaultRules()
        {
            // Economy rules
            RegisterRule(new IntegrityRule
            {
                RuleId = "economy.gold.range",
                Name = "Gold Range Check",
                Path = "economy.gold",
                Type = IntegrityRuleType.Range,
                MinValue = 0,
                MaxValue = 999999999,
                AutoCorrect = v => v is double d 
                    ? Math.Clamp(d, 0, 999999999) 
                    : 0.0
            });

            // Progression rules
            RegisterRule(new IntegrityRule
            {
                RuleId = "progression.level.range",
                Name = "Level Range Check",
                Path = "progression.level",
                Type = IntegrityRuleType.Range,
                MinValue = 1,
                MaxValue = 100,
                AutoCorrect = v => v is double d 
                    ? Math.Clamp(d, 1, 100) 
                    : 1.0
            });

            RegisterRule(new IntegrityRule
            {
                RuleId = "progression.xp.non_negative",
                Name = "XP Non-Negative Check",
                Path = "progression.xp",
                Type = IntegrityRuleType.Range,
                MinValue = 0,
                AutoCorrect = v => v is double d && d < 0 ? 0.0 : v
            });

            // Combat stats
            RegisterRule(new IntegrityRule
            {
                RuleId = "combat.hp.positive",
                Name = "HP Positive Check",
                Path = "combat.hp",
                Type = IntegrityRuleType.Range,
                MinValue = 0,
                AutoCorrect = v => v is double d && d < 0 ? 0.0 : v
            });

            // Custom validation for item quantities
            RegisterRule(new IntegrityRule
            {
                RuleId = "inventory.quantities.integer",
                Name = "Item Quantities Integer Check",
                Description = "Item quantities must be non-negative integers",
                Path = "inventory.quantities",
                Type = IntegrityRuleType.Custom,
                CustomValidator = v =>
                {
                    if (v is Dictionary<string, object> quantities)
                    {
                        return quantities.Values.All(q =>
                            q is int i && i >= 0 ||
                            q is double d && d >= 0 && d == Math.Floor(d));
                    }
                    return true;
                }
            });
        }
    }
}
