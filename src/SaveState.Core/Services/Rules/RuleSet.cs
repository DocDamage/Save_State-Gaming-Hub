using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveState.Core.Services.Rules
{
    /// <summary>
    /// Declarative rule definitions.
    /// </summary>
    public class GameContext
    {
        public Dictionary<string, bool> Flags { get; set; } = new();
        public Dictionary<string, int> Counters { get; set; } = new();
        public Dictionary<string, string> State { get; set; } = new();
        public string? CurrentAction { get; set; }
        public string? Actor { get; set; }
        public string? Target { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class Rule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Func<GameContext, bool> Condition { get; set; } = _ => true;
        public string ViolationMessage { get; set; } = string.Empty;
        public RuleSeverity Severity { get; set; } = RuleSeverity.Warning;
        public RuleCategory Category { get; set; } = RuleCategory.General;
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 0;
    }

    public enum RuleSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public enum RuleCategory
    {
        General,
        Combat,
        Economy,
        Quest,
        Social,
        Movement,
        Inventory
    }

    public class RuleSet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public List<Rule> Rules { get; set; } = new();
        public bool IsActive { get; set; } = true;

        public void AddRule(Rule rule) => Rules.Add(rule);

        public void AddRule(string name, Func<GameContext, bool> condition, string violationMessage,
            RuleSeverity severity = RuleSeverity.Warning, RuleCategory category = RuleCategory.General)
        {
            Rules.Add(new Rule
            {
                Name = name,
                Condition = condition,
                ViolationMessage = violationMessage,
                Severity = severity,
                Category = category
            });
        }
    }

    public static class CommonRules
    {
        public static RuleSet CreateCombatRules() => new RuleSet
        {
            Name = "Combat Rules",
            Rules = new List<Rule>
            {
                new() {
                    Name = "TargetMustBeAlive",
                    Condition = ctx => !ctx.Flags.TryGetValue($"{ctx.Target}_DEAD", out var dead) || !dead,
                    ViolationMessage = "Cannot attack a dead target",
                    Severity = RuleSeverity.Error,
                    Category = RuleCategory.Combat
                },
                new() {
                    Name = "MustHaveWeaponEquipped",
                    Condition = ctx => ctx.Flags.TryGetValue("HAS_WEAPON_EQUIPPED", out var has) && has,
                    ViolationMessage = "No weapon equipped",
                    Severity = RuleSeverity.Warning,
                    Category = RuleCategory.Combat
                },
                new() {
                    Name = "SufficientActionPoints",
                    Condition = ctx => ctx.Counters.TryGetValue("ACTION_POINTS", out var ap) && ap > 0,
                    ViolationMessage = "Not enough action points",
                    Severity = RuleSeverity.Error,
                    Category = RuleCategory.Combat
                }
            }
        };

        public static RuleSet CreateEconomyRules() => new RuleSet
        {
            Name = "Economy Rules",
            Rules = new List<Rule>
            {
                new() {
                    Name = "SufficientGold",
                    Condition = ctx => {
                        var cost = ctx.Counters.TryGetValue("TRANSACTION_COST", out var c) ? c : 0;
                        var gold = ctx.Counters.TryGetValue("GOLD", out var g) ? g : 0;
                        return gold >= cost;
                    },
                    ViolationMessage = "Insufficient gold",
                    Severity = RuleSeverity.Error,
                    Category = RuleCategory.Economy
                },
                new() {
                    Name = "InventorySpace",
                    Condition = ctx => {
                        var items = ctx.Counters.TryGetValue("INVENTORY_COUNT", out var c) ? c : 0;
                        var max = ctx.Counters.TryGetValue("INVENTORY_MAX", out var m) ? m : 100;
                        return items < max;
                    },
                    ViolationMessage = "Inventory full",
                    Severity = RuleSeverity.Error,
                    Category = RuleCategory.Inventory
                }
            }
        };

        public static RuleSet CreateQuestRules() => new RuleSet
        {
            Name = "Quest Rules",
            Rules = new List<Rule>
            {
                new() {
                    Name = "QuestPrerequisites",
                    Condition = ctx => {
                        var questId = ctx.State.TryGetValue("QUEST_ID", out var q) ? q : "";
                        var prereqKey = $"PREREQ_{questId}_MET";
                        return !ctx.Flags.TryGetValue(prereqKey, out var met) || met;
                    },
                    ViolationMessage = "Quest prerequisites not met",
                    Severity = RuleSeverity.Error,
                    Category = RuleCategory.Quest
                }
            }
        };
    }
}
