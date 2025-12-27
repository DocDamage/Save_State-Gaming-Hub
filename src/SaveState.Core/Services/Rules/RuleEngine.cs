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
        
        // Resolution
        void RegisterResolver(ActionResolver resolver);
        ResolutionResult ResolveAction(ActionProposal proposal, GameContext context);
    }

    public class RuleEngine : IRuleEngine
    {
        private readonly Dictionary<string, RuleSet> _ruleSets = new();
        private readonly Dictionary<string, DateTime> _cooldowns = new();
        private readonly Dictionary<string, ActionResolver> _resolvers = new();


        public RuleEngine()
        {
            // Register default rules
            RegisterRuleSet(CommonRules.CreateCombatRules());
            RegisterRuleSet(CommonRules.CreateEconomyRules());
            RegisterRuleSet(CommonRules.CreateQuestRules());
            
            // Register default resolvers (placeholders for now)
            RegisterDefaultResolvers();
        }

        private void RegisterDefaultResolvers()
        {
            // QUEST_ACCEPT
            RegisterResolver(new ActionResolver 
            { 
                ActionType = "QUEST_ACCEPT",
                Resolve = ctx => 
                {
                    var questId = ctx.State.TryGetValue("QUEST_ID", out var q) ? q : "UNKNOWN_QUEST";
                    
                    var result = new ResolutionResult { IsAllowed = true, Success = true };
                    result.OutcomeDescription = $"Accepted quest: {questId}";
                    
                    result.StateDiff.FlagUpdates[$"QUEST_{questId}_ACTIVE"] = true;
                    result.StateDiff.CounterUpdates["ACTIVE_QUEST_COUNT"] = 1; // +1 (relative update logic would be in applicator)
                    
                    return result; 
                }
            });

            // QUEST_COMPLETE
            RegisterResolver(new ActionResolver 
            { 
                ActionType = "QUEST_COMPLETE",
                Resolve = ctx => 
                {
                    var questId = ctx.State.TryGetValue("QUEST_ID", out var q) ? q : "UNKNOWN_QUEST";

                    var result = new ResolutionResult { IsAllowed = true, Success = true };
                    result.OutcomeDescription = $"Completed quest: {questId}";

                    result.StateDiff.FlagUpdates[$"QUEST_{questId}_ACTIVE"] = false;
                    result.StateDiff.FlagUpdates[$"QUEST_{questId}_COMPLETED"] = true;
                    result.StateDiff.CounterUpdates["ACTIVE_QUEST_COUNT"] = -1; // -1
                    
                    // Simple reward logic placeholder
                    result.StateDiff.CounterUpdates["PLAYER_XP"] = 100;

                    return result; 
                }
            });
        }


        public void RegisterRuleSet(RuleSet ruleSet)
        {
            _ruleSets[ruleSet.Id] = ruleSet;
        }

        public void UnregisterRuleSet(string ruleSetId)
        {
            _ruleSets.Remove(ruleSetId);
        }

        public void RegisterResolver(ActionResolver resolver)
        {
            _resolvers[resolver.ActionType] = resolver;
        }

        public ResolutionResult ResolveAction(ActionProposal proposal, GameContext context)
        {
            // 1. Validate
            // We map proposal to context for validation
            context.CurrentAction = proposal.ActionType;
            context.Actor = proposal.ActorId;
            context.Target = proposal.TargetId;
            
            var validation = Validate(context);
            if (!validation.IsValid)
            {
                return new ResolutionResult 
                { 
                    ActionId = proposal.ActionId,
                    IsAllowed = false, 
                    Success = false, 
                    FailureReason = validation.Summary,
                    EventsEmitted = validation.Violations.Select(v => v.Message).ToList()
                };
            }

            // 2. Resolve
            if (_resolvers.TryGetValue(proposal.ActionType, out var resolver))
            {
                var result = resolver.Resolve(context);
                result.ActionId = proposal.ActionId;
                result.IsAllowed = true;
                return result;
            }

            // Default: If no specific resolver, we assume generic success if valid, 
            // but no state changes unless specified broadly.
            return new ResolutionResult 
            { 
                ActionId = proposal.ActionId,
                IsAllowed = true, 
                Success = true, 
                OutcomeDescription = "Action allowed but no specific resolution logic defined." 
            };
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
