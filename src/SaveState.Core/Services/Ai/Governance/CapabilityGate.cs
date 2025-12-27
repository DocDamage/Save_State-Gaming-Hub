using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Governance
{
    /// <summary>
    /// Capability gating system for AI actions.
    /// Determines what an AI is allowed to do based on context.
    /// </summary>
    public interface ICapabilityGate
    {
        /// <summary>
        /// Check if a capability is allowed in the given context
        /// </summary>
        Task<CapabilityCheckResult> CheckCapabilityAsync(AiCapability capability, AiPermissionContext context);

        /// <summary>
        /// Check multiple capabilities at once
        /// </summary>
        Task<Dictionary<AiCapability, CapabilityCheckResult>> CheckCapabilitiesAsync(
            IEnumerable<AiCapability> capabilities, AiPermissionContext context);

        /// <summary>
        /// Get all available capabilities for a context
        /// </summary>
        Task<IEnumerable<AiCapability>> GetAvailableCapabilitiesAsync(AiPermissionContext context);

        /// <summary>
        /// Register a permission rule
        /// </summary>
        void RegisterRule(PermissionRule rule);

        /// <summary>
        /// Remove a permission rule
        /// </summary>
        void RemoveRule(string ruleId);

        /// <summary>
        /// Get all registered rules
        /// </summary>
        IEnumerable<PermissionRule> GetRules();
    }

    /// <summary>
    /// Default implementation of capability gating
    /// </summary>
    public class CapabilityGate : ICapabilityGate
    {
        private readonly ConcurrentDictionary<string, PermissionRule> _rules = new();
        private readonly ConcurrentDictionary<string, RateLimitTracker> _rateLimiters = new();
        private readonly List<Func<AiCapability, AiPermissionContext, CapabilityCheckResult?>> _customChecks = new();

        public CapabilityGate()
        {
            RegisterDefaultRules();
        }

        public async Task<CapabilityCheckResult> CheckCapabilityAsync(AiCapability capability, AiPermissionContext context)
        {
            // First, check custom rules
            foreach (var check in _customChecks)
            {
                var result = check(capability, context);
                if (result != null && !result.IsAllowed)
                {
                    return result;
                }
            }

            // Find applicable rules
            var applicableRules = _rules.Values
                .Where(r => r.Capability == capability)
                .ToList();

            if (!applicableRules.Any())
            {
                // No rules defined = capability is allowed by default
                return CapabilityCheckResult.Allowed(capability);
            }

            foreach (var rule in applicableRules)
            {
                // Check if globally disabled
                if (rule.IsDisabledGlobally)
                {
                    return CapabilityCheckResult.Denied(capability,
                        $"Capability '{capability}' is currently disabled globally.",
                        "Please try again later or contact support.");
                }

                // Check tier requirement
                if (context.Tier < rule.MinimumTier)
                {
                    return CapabilityCheckResult.Denied(capability,
                        $"Capability '{capability}' requires {rule.MinimumTier} tier or higher.",
                        $"Upgrade to {rule.MinimumTier} to unlock this feature.");
                }

                // Check game mode restrictions
                if (rule.AllowedModes.Any() && !rule.AllowedModes.Contains(context.Mode))
                {
                    return CapabilityCheckResult.Denied(capability,
                        $"Capability '{capability}' is not available in {context.Mode} mode.",
                        $"Available in: {string.Join(", ", rule.AllowedModes)}");
                }

                // Check service restrictions
                if (rule.AllowedServices.Any() && !rule.AllowedServices.Contains(context.RequestingService))
                {
                    return CapabilityCheckResult.Denied(capability,
                        $"Service '{context.RequestingService}' cannot use capability '{capability}'.",
                        null);
                }

                // Check rate limits
                if (rule.RateLimitPerMinute.HasValue || rule.RateLimitPerHour.HasValue || rule.RateLimitPerDay.HasValue)
                {
                    var rateLimitKey = $"{context.UserId}:{capability}";
                    var tracker = _rateLimiters.GetOrAdd(rateLimitKey, _ => new RateLimitTracker());

                    if (!await tracker.TryConsumeAsync(rule))
                    {
                        return CapabilityCheckResult.Denied(capability,
                            "Rate limit exceeded for this capability.",
                            "Please wait before trying again.");
                    }
                }
            }

            return CapabilityCheckResult.Allowed(capability);
        }

        public async Task<Dictionary<AiCapability, CapabilityCheckResult>> CheckCapabilitiesAsync(
            IEnumerable<AiCapability> capabilities, AiPermissionContext context)
        {
            var results = new Dictionary<AiCapability, CapabilityCheckResult>();
            foreach (var capability in capabilities)
            {
                results[capability] = await CheckCapabilityAsync(capability, context);
            }
            return results;
        }

        public Task<IEnumerable<AiCapability>> GetAvailableCapabilitiesAsync(AiPermissionContext context)
        {
            var allCapabilities = Enum.GetValues<AiCapability>();
            var available = new List<AiCapability>();

            foreach (var capability in allCapabilities)
            {
                var result = CheckCapabilityAsync(capability, context).GetAwaiter().GetResult();
                if (result.IsAllowed)
                {
                    available.Add(capability);
                }
            }

            return Task.FromResult<IEnumerable<AiCapability>>(available);
        }

        public void RegisterRule(PermissionRule rule)
        {
            _rules[rule.RuleId] = rule;
        }

        public void RemoveRule(string ruleId)
        {
            _rules.TryRemove(ruleId, out _);
        }

        public IEnumerable<PermissionRule> GetRules() => _rules.Values;

        /// <summary>
        /// Add a custom capability check function
        /// </summary>
        public void AddCustomCheck(Func<AiCapability, AiPermissionContext, CapabilityCheckResult?> check)
        {
            _customChecks.Add(check);
        }

        private void RegisterDefaultRules()
        {
            // Basic capabilities - available to all
            RegisterRule(new PermissionRule
            {
                Name = "BasicChat",
                Capability = AiCapability.BasicChat,
                MinimumTier = UserTier.Free
            });

            RegisterRule(new PermissionRule
            {
                Name = "GameAnalysis",
                Capability = AiCapability.GameAnalysis,
                MinimumTier = UserTier.Free,
                RateLimitPerMinute = 10
            });

            // Content generation - requires Premium
            RegisterRule(new PermissionRule
            {
                Name = "TextGeneration",
                Capability = AiCapability.TextGeneration,
                MinimumTier = UserTier.Premium,
                RateLimitPerHour = 100
            });

            RegisterRule(new PermissionRule
            {
                Name = "ImageGeneration",
                Capability = AiCapability.ImageGeneration,
                MinimumTier = UserTier.Premium,
                RateLimitPerDay = 50
            });

            // Game state modification - very restricted
            RegisterRule(new PermissionRule
            {
                Name = "ModifyGameState",
                Capability = AiCapability.ModifyGameState,
                MinimumTier = UserTier.Developer,
                AllowedModes = new List<GameMode> { GameMode.Sandbox, GameMode.Creative },
                AllowedServices = new List<AiServiceType> { AiServiceType.Developer, AiServiceType.TestHarness }
            });

            // Canon modification - highly restricted
            RegisterRule(new PermissionRule
            {
                Name = "ModifyCanon",
                Capability = AiCapability.ModifyCanon,
                MinimumTier = UserTier.Admin,
                RequiresExplicitGrant = true
            });

            // NPC capabilities - mode-dependent
            RegisterRule(new PermissionRule
            {
                Name = "NpcDialogue",
                Capability = AiCapability.NpcDialogue,
                MinimumTier = UserTier.Free,
                AllowedModes = new List<GameMode> 
                { 
                    GameMode.Story, GameMode.Arcade, GameMode.Creative, 
                    GameMode.Sandbox, GameMode.Default 
                }
            });

            // Tool execution - Pro+ only
            RegisterRule(new PermissionRule
            {
                Name = "ToolExecution",
                Capability = AiCapability.ToolExecution,
                MinimumTier = UserTier.Pro,
                AllowedServices = new List<AiServiceType> 
                { 
                    AiServiceType.Orchestrator, AiServiceType.Developer 
                },
                RateLimitPerMinute = 30
            });

            // Test capabilities - Developer only
            RegisterRule(new PermissionRule
            {
                Name = "TestSimulation",
                Capability = AiCapability.TestSimulation,
                MinimumTier = UserTier.Developer
            });

            RegisterRule(new PermissionRule
            {
                Name = "StressTest",
                Capability = AiCapability.StressTest,
                MinimumTier = UserTier.Developer,
                RateLimitPerDay = 10
            });
        }
    }

    /// <summary>
    /// Tracks rate limits for capability usage
    /// </summary>
    internal class RateLimitTracker
    {
        private readonly Queue<DateTime> _minuteWindow = new();
        private readonly Queue<DateTime> _hourWindow = new();
        private readonly Queue<DateTime> _dayWindow = new();
        private readonly object _lock = new();

        public Task<bool> TryConsumeAsync(PermissionRule rule)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                CleanupWindows(now);

                // Check minute limit
                if (rule.RateLimitPerMinute.HasValue && _minuteWindow.Count >= rule.RateLimitPerMinute.Value)
                {
                    return Task.FromResult(false);
                }

                // Check hour limit
                if (rule.RateLimitPerHour.HasValue && _hourWindow.Count >= rule.RateLimitPerHour.Value)
                {
                    return Task.FromResult(false);
                }

                // Check day limit
                if (rule.RateLimitPerDay.HasValue && _dayWindow.Count >= rule.RateLimitPerDay.Value)
                {
                    return Task.FromResult(false);
                }

                // Record usage
                _minuteWindow.Enqueue(now);
                _hourWindow.Enqueue(now);
                _dayWindow.Enqueue(now);

                return Task.FromResult(true);
            }
        }

        private void CleanupWindows(DateTime now)
        {
            var minuteAgo = now.AddMinutes(-1);
            var hourAgo = now.AddHours(-1);
            var dayAgo = now.AddDays(-1);

            while (_minuteWindow.Count > 0 && _minuteWindow.Peek() < minuteAgo)
                _minuteWindow.Dequeue();

            while (_hourWindow.Count > 0 && _hourWindow.Peek() < hourAgo)
                _hourWindow.Dequeue();

            while (_dayWindow.Count > 0 && _dayWindow.Peek() < dayAgo)
                _dayWindow.Dequeue();
        }
    }
}
