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
                // 1. Prerequisites Check
                new() {
                    Name = "QuestPrerequisites",
                    Condition = ctx => {
                        var questId = ctx.State.TryGetValue("QUEST_ID", out var q) ? q : "";
                        var prereqKey = $"PREREQ_{questId}_MET";
                        // If flag doesn't exist, assume met (unless specific negative logic needed)
                        // In reality, this would lookup a Quest Definition to see WHAT the prereq is.
                        // For generic rule, act if the specific 'MET' flag is false.
                        return !ctx.Flags.TryGetValue(prereqKey, out var met) || met;
                    },
                    ViolationMessage = "Quest prerequisites not met",
                    Severity = RuleSeverity.Error,
                    Category = RuleCategory.Quest
                },

                // 2. Already Completed Check (Repeatability)
                new() {
                    Name = "QuestNotAlreadyCompleted",
                    Condition = ctx => {
                        var questId = ctx.State.TryGetValue("QUEST_ID", out var q) ? q : "";
                        // If QUEST_{ID}_COMPLETED is true, fail unless IS_REPEATABLE is true
                        if (ctx.Flags.TryGetValue($"QUEST_{questId}_COMPLETED", out var completed) && completed)
                        {
                            return ctx.Flags.TryGetValue($"QUEST_{questId}_REPEATABLE", out var repeatable) && repeatable;
                        }
                        return true;
                    },
                    ViolationMessage = "Quest already completed and not repeatable",
                    Severity = RuleSeverity.Error,
                    Category = RuleCategory.Quest
                },

                // 3. Level Requirement
                new() {
                    Name = "QuestLevelRequirement",
                    Condition = ctx => {
                         var questId = ctx.State.TryGetValue("QUEST_ID", out var q) ? q : "";
                         // Lookup required level from context counters (assuming injection)
                         // e.g., QUEST_{ID}_MIN_LEVEL
                         if (ctx.Counters.TryGetValue($"QUEST_{questId}_MIN_LEVEL", out var minLevel))
                         {
                             var playerLevel = ctx.Counters.TryGetValue("PLAYER_LEVEL", out var pl) ? pl : 1;
                             return playerLevel >= minLevel;
                         }
                         return true;
                    },
                    ViolationMessage = "Player level too low for this quest",
                    Severity = RuleSeverity.Warning, // Allow picking up but warn? Or Error? Let's say Error.
                    Category = RuleCategory.Quest
                },

                // 4. Quest Log Capacity
                new() {
                    Name = "QuestLogCapacity",
                    Condition = ctx => {
                        // Only relevant for 'Accept' action
                        if (ctx.CurrentAction != "QUEST_ACCEPT") return true;

                        var activeCount = ctx.Counters.TryGetValue("ACTIVE_QUEST_COUNT", out var c) ? c : 0;
                        var maxQuests = ctx.Counters.TryGetValue("MAX_QUEST_SLOTS", out var m) ? m : 20;
                        return activeCount < maxQuests;
                    },
                    ViolationMessage = "Quest log is full",
                    Severity = RuleSeverity.Error,
                    Category = RuleCategory.Quest
                }
            }
        };
    }
}
