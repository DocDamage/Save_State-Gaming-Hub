using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SaveState.Core.Services.Rules
{
    /// <summary>
    /// Pattern: LLM proposes → Rules validate → LLM narrates.
    /// Returns: Approved, Denied, Modified
    /// </summary>
    public enum ActionStatus
    {
        Approved,
        Denied,
        Modified,
        Pending
    }

    public class ProposedAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ActionType { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string? Target { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string? Description { get; set; }
        public DateTime ProposedAt { get; set; } = DateTime.UtcNow;
    }

    public class ActionValidationResult
    {
        public ActionStatus Status { get; set; }
        public ProposedAction OriginalAction { get; set; } = null!;
        public ProposedAction? ModifiedAction { get; set; }
        public List<RuleViolation> Violations { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
        public Dictionary<string, object> Effects { get; set; } = new();
        
        public bool IsApproved => Status == ActionStatus.Approved || Status == ActionStatus.Modified;
    }

    public interface IActionValidator
    {
        ActionValidationResult Validate(ProposedAction action, GameContext context);
        ActionValidationResult ValidateAndApply(ProposedAction action, GameContext context);
    }

    public class ActionValidator : IActionValidator
    {
        private readonly IRuleEngine _ruleEngine;
        private readonly Dictionary<string, Func<ProposedAction, GameContext, (bool, string, Dictionary<string, object>?)>> _actionHandlers = new();

        public ActionValidator(IRuleEngine? ruleEngine = null)
        {
            _ruleEngine = ruleEngine ?? new RuleEngine();
            RegisterDefaultHandlers();
        }

        private void RegisterDefaultHandlers()
        {
            _actionHandlers["attack"] = (action, ctx) =>
            {
                ctx.CurrentAction = "attack";
                ctx.Actor = action.Actor;
                ctx.Target = action.Target;
                
                var validation = _ruleEngine.Validate(ctx, RuleCategory.Combat);
                if (!validation.IsValid)
                    return (false, validation.Summary, null);

                var damage = CalculateDamage(action, ctx);
                return (true, $"Attack deals {damage} damage", new Dictionary<string, object> { ["damage"] = damage });
            };

            _actionHandlers["buy"] = (action, ctx) =>
            {
                ctx.CurrentAction = "buy";
                if (action.Parameters.TryGetValue("cost", out var costObj))
                    ctx.Counters["TRANSACTION_COST"] = Convert.ToInt32(costObj);

                var validation = _ruleEngine.Validate(ctx, RuleCategory.Economy);
                if (!validation.IsValid)
                    return (false, validation.Summary, null);

                return (true, "Transaction approved", null);
            };

            _actionHandlers["use_ability"] = (action, ctx) =>
            {
                var abilityId = action.Parameters.TryGetValue("ability", out var a) ? a.ToString() : "";
                
                // Check cooldowns and resources
                var manaCost = action.Parameters.TryGetValue("mana_cost", out var m) ? Convert.ToInt32(m) : 0;
                var currentMana = ctx.Counters.TryGetValue("MANA", out var cm) ? cm : 0;
                
                if (currentMana < manaCost)
                    return (false, "Insufficient mana", null);

                return (true, $"Ability {abilityId} activated", new Dictionary<string, object> 
                { 
                    ["mana_spent"] = manaCost 
                });
            };

            _actionHandlers["move"] = (action, ctx) =>
            {
                var destination = action.Parameters.TryGetValue("destination", out var d) ? d.ToString() : "";
                var unlockKey = $"AREA_UNLOCKED_{destination?.ToUpperInvariant()}";
                
                if (ctx.Flags.TryGetValue(unlockKey, out var unlocked) && !unlocked)
                    return (false, $"Area '{destination}' is not unlocked", null);

                return (true, $"Moving to {destination}", null);
            };
        }

        public ActionValidationResult Validate(ProposedAction action, GameContext context)
        {
            var result = new ActionValidationResult
            {
                OriginalAction = action,
                Status = ActionStatus.Pending
            };

            // Check if we have a handler for this action type
            if (!_actionHandlers.TryGetValue(action.ActionType.ToLowerInvariant(), out var handler))
            {
                // Generic validation for unknown actions
                var genericValidation = _ruleEngine.Validate(context);
                result.Status = genericValidation.IsValid ? ActionStatus.Approved : ActionStatus.Denied;
                result.Violations = genericValidation.Violations;
                result.Reason = genericValidation.Summary;
                return result;
            }

            // Run specific handler
            var (approved, reason, effects) = handler(action, context);
            
            result.Status = approved ? ActionStatus.Approved : ActionStatus.Denied;
            result.Reason = reason;
            
            if (effects != null)
                result.Effects = effects;

            return result;
        }

        public ActionValidationResult ValidateAndApply(ProposedAction action, GameContext context)
        {
            var result = Validate(action, context);
            
            if (result.IsApproved)
            {
                // Apply effects to context
                foreach (var (key, value) in result.Effects)
                {
                    if (value is int intVal)
                        context.Counters[key] = intVal;
                    else if (value is bool boolVal)
                        context.Flags[key] = boolVal;
                    else
                        context.State[key] = value.ToString() ?? "";
                }
            }

            return result;
        }

        private int CalculateDamage(ProposedAction action, GameContext context)
        {
            var baseDamage = action.Parameters.TryGetValue("base_damage", out var d) ? Convert.ToInt32(d) : 10;
            var strength = context.Counters.TryGetValue("STRENGTH", out var s) ? s : 10;
            var critChance = context.Counters.TryGetValue("CRIT_CHANCE", out var c) ? c : 5;

            var isCrit = new Random().Next(100) < critChance;
            var damage = baseDamage + (strength / 2);
            
            if (isCrit) damage *= 2;

            return damage;
        }

        public void RegisterActionHandler(string actionType, 
            Func<ProposedAction, GameContext, (bool, string, Dictionary<string, object>?)> handler)
        {
            _actionHandlers[actionType.ToLowerInvariant()] = handler;
        }
    }
}
