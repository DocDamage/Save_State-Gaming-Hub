using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Governance
{
    /// <summary>
    /// Hard safety rails at the tool level.
    /// These are non-negotiable blocks that cannot be overridden by feature flags or permissions.
    /// Think of this as the "circuit breaker" for dangerous operations.
    /// </summary>
    public interface ISafetyRails
    {
        /// <summary>
        /// Check if an action is blocked by safety rails
        /// </summary>
        SafetyCheckResult CheckAction(SafetyAction action);

        /// <summary>
        /// Validate content against safety rules
        /// </summary>
        SafetyCheckResult ValidateContent(string content, ContentType type);

        /// <summary>
        /// Check if a tool invocation is safe
        /// </summary>
        SafetyCheckResult CheckToolInvocation(string toolName, Dictionary<string, object> parameters);

        /// <summary>
        /// Register a custom safety rule
        /// </summary>
        void RegisterRule(SafetyRule rule);

        /// <summary>
        /// Get all violations for the current session
        /// </summary>
        IEnumerable<SafetyViolation> GetViolations(string? sessionId = null);

        /// <summary>
        /// Clear violations for a session
        /// </summary>
        void ClearViolations(string sessionId);
    }

    /// <summary>
    /// Result of a safety check
    /// </summary>
    public class SafetyCheckResult
    {
        public bool IsSafe { get; set; }
        public SafetySeverity Severity { get; set; } = SafetySeverity.None;
        public string? ViolationCode { get; set; }
        public string? Message { get; set; }
        public string? Suggestion { get; set; }
        public bool WasBlocked { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();

        public static SafetyCheckResult Safe() => new() { IsSafe = true };
        
        public static SafetyCheckResult Blocked(string code, string message, SafetySeverity severity = SafetySeverity.Critical)
            => new()
            {
                IsSafe = false,
                WasBlocked = true,
                ViolationCode = code,
                Message = message,
                Severity = severity
            };
    }

    /// <summary>
    /// Severity levels for safety violations
    /// </summary>
    public enum SafetySeverity
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Types of content to validate
    /// </summary>
    public enum ContentType
    {
        UserInput,
        AiOutput,
        SystemPrompt,
        GameData,
        NpcDialogue,
        QuestContent,
        LoreText,
        CombatNarration,
        GeneratedImage
    }

    /// <summary>
    /// An action to be checked for safety
    /// </summary>
    public class SafetyAction
    {
        public string ActionType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string? Target { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public AiPermissionContext? Context { get; set; }
    }

    /// <summary>
    /// A recorded safety violation
    /// </summary>
    public class SafetyViolation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SessionId { get; set; } = string.Empty;
        public string ViolationCode { get; set; } = string.Empty;
        public SafetySeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ActionType { get; set; }
        public string? Content { get; set; }
        public bool WasBlocked { get; set; }
    }

    /// <summary>
    /// A safety rule definition
    /// </summary>
    public class SafetyRule
    {
        public string RuleId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SafetyRuleType Type { get; set; }
        public SafetySeverity Severity { get; set; } = SafetySeverity.High;
        
        /// <summary>
        /// Regex pattern for content matching (if applicable)
        /// </summary>
        public string? Pattern { get; set; }
        
        /// <summary>
        /// Keywords to check for (if applicable)
        /// </summary>
        public List<string> Keywords { get; set; } = new();
        
        /// <summary>
        /// Action types this rule applies to
        /// </summary>
        public List<string> ActionTypes { get; set; } = new();
        
        /// <summary>
        /// Tool names this rule applies to
        /// </summary>
        public List<string> ToolNames { get; set; } = new();
        
        /// <summary>
        /// Custom validation function
        /// </summary>
        public Func<SafetyAction, bool>? CustomValidator { get; set; }
        
        /// <summary>
        /// Whether this rule is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Types of safety rules
    /// </summary>
    public enum SafetyRuleType
    {
        ContentFilter,
        ActionBlock,
        ToolRestriction,
        RateLimit,
        PatternMatch,
        Custom
    }

    /// <summary>
    /// Default implementation of safety rails
    /// </summary>
    public class SafetyRails : ISafetyRails
    {
        private readonly ConcurrentDictionary<string, SafetyRule> _rules = new();
        private readonly ConcurrentDictionary<string, List<SafetyViolation>> _violations = new();
        private readonly List<Regex> _compiledPatterns = new();

        public SafetyRails()
        {
            RegisterDefaultRules();
        }

        public SafetyCheckResult CheckAction(SafetyAction action)
        {
            foreach (var rule in _rules.Values.Where(r => r.IsActive))
            {
                if (rule.Type == SafetyRuleType.ActionBlock &&
                    rule.ActionTypes.Contains(action.ActionType, StringComparer.OrdinalIgnoreCase))
                {
                    RecordViolation(action.Context?.SessionId, rule, action.ActionType);
                    return SafetyCheckResult.Blocked(
                        rule.RuleId,
                        $"Action '{action.ActionType}' is blocked: {rule.Description}",
                        rule.Severity
                    );
                }

                if (rule.CustomValidator != null && !rule.CustomValidator(action))
                {
                    RecordViolation(action.Context?.SessionId, rule, action.ActionType);
                    return SafetyCheckResult.Blocked(
                        rule.RuleId,
                        $"Action failed safety check: {rule.Description}",
                        rule.Severity
                    );
                }
            }

            return SafetyCheckResult.Safe();
        }

        public SafetyCheckResult ValidateContent(string content, ContentType type)
        {
            if (string.IsNullOrEmpty(content))
            {
                return SafetyCheckResult.Safe();
            }

            var lowerContent = content.ToLowerInvariant();

            foreach (var rule in _rules.Values.Where(r => r.IsActive && r.Type == SafetyRuleType.ContentFilter))
            {
                // Check keywords
                if (rule.Keywords.Any(k => lowerContent.Contains(k.ToLowerInvariant())))
                {
                    RecordViolation(null, rule, content);
                    return SafetyCheckResult.Blocked(
                        rule.RuleId,
                        $"Content violates safety rule: {rule.Name}",
                        rule.Severity
                    );
                }

                // Check patterns
                if (!string.IsNullOrEmpty(rule.Pattern))
                {
                    try
                    {
                        var regex = new Regex(rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        if (regex.IsMatch(content))
                        {
                            RecordViolation(null, rule, content);
                            return SafetyCheckResult.Blocked(
                                rule.RuleId,
                                $"Content matches blocked pattern: {rule.Name}",
                                rule.Severity
                            );
                        }
                    }
                    catch
                    {
                        // Invalid regex, skip
                    }
                }
            }

            return SafetyCheckResult.Safe();
        }

        public SafetyCheckResult CheckToolInvocation(string toolName, Dictionary<string, object> parameters)
        {
            foreach (var rule in _rules.Values.Where(r => r.IsActive && r.Type == SafetyRuleType.ToolRestriction))
            {
                if (rule.ToolNames.Contains(toolName, StringComparer.OrdinalIgnoreCase))
                {
                    RecordViolation(null, rule, toolName);
                    return SafetyCheckResult.Blocked(
                        rule.RuleId,
                        $"Tool '{toolName}' is restricted: {rule.Description}",
                        rule.Severity
                    );
                }
            }

            // Check for dangerous parameter patterns
            foreach (var param in parameters)
            {
                if (param.Value is string strValue)
                {
                    var contentCheck = ValidateContent(strValue, ContentType.UserInput);
                    if (!contentCheck.IsSafe)
                    {
                        return contentCheck;
                    }
                }
            }

            return SafetyCheckResult.Safe();
        }

        public void RegisterRule(SafetyRule rule)
        {
            _rules[rule.RuleId] = rule;
        }

        public IEnumerable<SafetyViolation> GetViolations(string? sessionId = null)
        {
            if (sessionId == null)
            {
                return _violations.Values.SelectMany(v => v);
            }

            return _violations.TryGetValue(sessionId, out var violations) 
                ? violations 
                : Enumerable.Empty<SafetyViolation>();
        }

        public void ClearViolations(string sessionId)
        {
            _violations.TryRemove(sessionId, out _);
        }

        private void RecordViolation(string? sessionId, SafetyRule rule, string? content)
        {
            var violation = new SafetyViolation
            {
                SessionId = sessionId ?? "unknown",
                ViolationCode = rule.RuleId,
                Severity = rule.Severity,
                Message = rule.Description,
                Content = content?.Length > 200 ? content[..200] + "..." : content,
                WasBlocked = true
            };

            var key = sessionId ?? "global";
            _violations.AddOrUpdate(
                key,
                _ => new List<SafetyViolation> { violation },
                (_, list) => { list.Add(violation); return list; }
            );
        }

        private void RegisterDefaultRules()
        {
            // === Action Blocks ===
            RegisterRule(new SafetyRule
            {
                RuleId = "action.delete_all_data",
                Name = "Block Mass Data Deletion",
                Description = "Prevents AI from deleting all user data",
                Type = SafetyRuleType.ActionBlock,
                Severity = SafetySeverity.Critical,
                ActionTypes = new List<string> { "delete_all_data", "wipe_database", "clear_all" }
            });

            RegisterRule(new SafetyRule
            {
                RuleId = "action.admin_override",
                Name = "Block Admin Override",
                Description = "Prevents AI from granting admin permissions",
                Type = SafetyRuleType.ActionBlock,
                Severity = SafetySeverity.Critical,
                ActionTypes = new List<string> { "grant_admin", "escalate_privileges", "bypass_auth" }
            });

            RegisterRule(new SafetyRule
            {
                RuleId = "action.canon_corruption",
                Name = "Block Canon Corruption",
                Description = "Prevents AI from corrupting canonical game state",
                Type = SafetyRuleType.ActionBlock,
                Severity = SafetySeverity.Critical,
                ActionTypes = new List<string> { "corrupt_canon", "override_lore", "force_state" }
            });

            // === Tool Restrictions ===
            RegisterRule(new SafetyRule
            {
                RuleId = "tool.file_system",
                Name = "Block Raw File System Access",
                Description = "Prevents AI from direct file system manipulation",
                Type = SafetyRuleType.ToolRestriction,
                Severity = SafetySeverity.Critical,
                ToolNames = new List<string> { "file_delete", "file_write_raw", "execute_command" }
            });

            RegisterRule(new SafetyRule
            {
                RuleId = "tool.network_raw",
                Name = "Block Raw Network Access",
                Description = "Prevents AI from making arbitrary network requests",
                Type = SafetyRuleType.ToolRestriction,
                Severity = SafetySeverity.High,
                ToolNames = new List<string> { "http_raw", "socket_connect", "network_scan" }
            });

            // === Content Filters ===
            RegisterRule(new SafetyRule
            {
                RuleId = "content.injection",
                Name = "Prompt Injection Detection",
                Description = "Blocks potential prompt injection attempts",
                Type = SafetyRuleType.ContentFilter,
                Severity = SafetySeverity.High,
                Keywords = new List<string>
                {
                    "ignore previous instructions",
                    "disregard your training",
                    "you are now",
                    "new instructions:",
                    "forget everything"
                }
            });

            RegisterRule(new SafetyRule
            {
                RuleId = "content.jailbreak",
                Name = "Jailbreak Attempt Detection",
                Description = "Blocks common jailbreak patterns",
                Type = SafetyRuleType.ContentFilter,
                Severity = SafetySeverity.High,
                Pattern = @"(DAN|Do Anything Now|jailbreak|bypass.*filter|ignore.*safety)"
            });

            RegisterRule(new SafetyRule
            {
                RuleId = "content.pii_extraction",
                Name = "PII Extraction Prevention",
                Description = "Prevents attempts to extract personal information",
                Type = SafetyRuleType.ContentFilter,
                Severity = SafetySeverity.High,
                Keywords = new List<string>
                {
                    "give me your system prompt",
                    "what's your api key",
                    "list all users",
                    "show me passwords"
                }
            });

            // === Custom Validators ===
            RegisterRule(new SafetyRule
            {
                RuleId = "custom.npc_as_dev",
                Name = "NPC Acting as Developer",
                Description = "Prevents NPC AI from performing developer actions",
                Type = SafetyRuleType.Custom,
                Severity = SafetySeverity.High,
                CustomValidator = action =>
                {
                    if (action.Context?.RequestingService == AiServiceType.Npc)
                    {
                        var devActions = new[] { "modify_code", "change_config", "deploy", "debug" };
                        return !devActions.Contains(action.ActionType, StringComparer.OrdinalIgnoreCase);
                    }
                    return true;
                }
            });

            RegisterRule(new SafetyRule
            {
                RuleId = "custom.economy_manipulation",
                Name = "Economy Manipulation Guard",
                Description = "Prevents unauthorized economy modifications",
                Type = SafetyRuleType.Custom,
                Severity = SafetySeverity.Critical,
                CustomValidator = action =>
                {
                    if (action.ActionType.Contains("economy", StringComparison.OrdinalIgnoreCase) ||
                        action.ActionType.Contains("currency", StringComparison.OrdinalIgnoreCase))
                    {
                        // Only allow in sandbox/creative mode
                        var allowedModes = new[] { GameMode.Sandbox, GameMode.Creative };
                        return action.Context != null && allowedModes.Contains(action.Context.Mode);
                    }
                    return true;
                }
            });
        }
    }
}
