using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveState.Core.Services.Rules
{
    /// <summary>
    /// Core rule validation system.
    /// - Combat resolution rules
    /// - Economy constraints  
    /// - Cooldown tracking
    /// - Quest prerequisite validation
    /// </summary>
    public class RuleViolation
    {
        public Rule Rule { get; set; } = null!;
        public string Message { get; set; } = string.Empty;
        public RuleSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<RuleViolation> Violations { get; set; } = new();
        public List<Rule> PassedRules { get; set; } = new();
        public string Summary => IsValid ? "Valid" : string.Join("; ", Violations.Select(v => v.Message));
    }

    public interface IRuleEngine
    {
        void RegisterRuleSet(RuleSet ruleSet);
        void UnregisterRuleSet(string ruleSetId);
        ValidationResult Validate(GameContext context, RuleCategory? category = null);
        IEnumerable<RuleSet> GetRuleSets();
        void SetRuleActive(string ruleId, bool active);
    }

    public class RuleEngine : IRuleEngine
    {
        private readonly Dictionary<string, RuleSet> _ruleSets = new();
        private readonly Dictionary<string, DateTime> _cooldowns = new();

        public RuleEngine()
        {
            // Register default rules
            RegisterRuleSet(CommonRules.CreateCombatRules());
            RegisterRuleSet(CommonRules.CreateEconomyRules());
            RegisterRuleSet(CommonRules.CreateQuestRules());
        }

        public void RegisterRuleSet(RuleSet ruleSet)
        {
            _ruleSets[ruleSet.Id] = ruleSet;
        }

        public void UnregisterRuleSet(string ruleSetId)
        {
            _ruleSets.Remove(ruleSetId);
        }

        public ValidationResult Validate(GameContext context, RuleCategory? category = null)
        {
            var result = new ValidationResult { IsValid = true };
            
            var applicableRules = _ruleSets.Values
                .Where(rs => rs.IsActive)
                .SelectMany(rs => rs.Rules)
                .Where(r => r.IsActive)
                .Where(r => !category.HasValue || r.Category == category.Value)
                .OrderByDescending(r => r.Priority);

            foreach (var rule in applicableRules)
            {
                try
                {
                    if (!rule.Condition(context))
                    {
                        result.Violations.Add(new RuleViolation
                        {
                            Rule = rule,
                            Message = rule.ViolationMessage,
                            Severity = rule.Severity
                        });

                        if (rule.Severity >= RuleSeverity.Error)
                        {
                            result.IsValid = false;
                        }
                    }
                    else
                    {
                        result.PassedRules.Add(rule);
                    }
                }
                catch (Exception ex)
                {
                    result.Violations.Add(new RuleViolation
                    {
                        Rule = rule,
                        Message = $"Rule evaluation error: {ex.Message}",
                        Severity = RuleSeverity.Warning
                    });
                }
            }

            return result;
        }

        public IEnumerable<RuleSet> GetRuleSets() => _ruleSets.Values;

        public void SetRuleActive(string ruleId, bool active)
        {
            foreach (var ruleSet in _ruleSets.Values)
            {
                var rule = ruleSet.Rules.FirstOrDefault(r => r.Id == ruleId);
                if (rule != null)
                {
                    rule.IsActive = active;
                    return;
                }
            }
        }

        public bool CheckCooldown(string actionId, TimeSpan cooldownDuration)
        {
            if (_cooldowns.TryGetValue(actionId, out var lastUse))
            {
                if (DateTime.UtcNow - lastUse < cooldownDuration)
                    return false;
            }
            _cooldowns[actionId] = DateTime.UtcNow;
            return true;
        }
    }
}
