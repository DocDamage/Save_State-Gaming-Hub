using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Governance
{
    /// <summary>
    /// Main AI Governance Service - orchestrates all governance components.
    /// This is the single entry point for all governance-related checks.
    /// </summary>
    public interface IAiGovernanceService
    {
        /// <summary>
        /// Check if an AI action is allowed
        /// </summary>
        Task<GovernanceDecision> CheckActionAsync(GovernanceRequest request);

        /// <summary>
        /// Quick check for a single capability
        /// </summary>
        Task<bool> CanPerformAsync(AiCapability capability, AiPermissionContext context);

        /// <summary>
        /// Check if a feature is enabled
        /// </summary>
        Task<bool> IsFeatureEnabledAsync(string featureKey, AiPermissionContext context);

        /// <summary>
        /// Validate content through safety rails
        /// </summary>
        SafetyCheckResult ValidateContent(string content, ContentType type);

        /// <summary>
        /// Get the capability gate instance
        /// </summary>
        ICapabilityGate CapabilityGate { get; }

        /// <summary>
        /// Get the feature flag service
        /// </summary>
        IFeatureFlagService FeatureFlags { get; }

        /// <summary>
        /// Get the safety rails instance
        /// </summary>
        ISafetyRails SafetyRails { get; }
    }

    /// <summary>
    /// Request for governance check
    /// </summary>
    public class GovernanceRequest
    {
        public AiPermissionContext Context { get; set; } = new();
        public string ActionType { get; set; } = string.Empty;
        public AiCapability? RequiredCapability { get; set; }
        public string? FeatureFlag { get; set; }
        public string? ContentToValidate { get; set; }
        public ContentType ContentType { get; set; } = ContentType.UserInput;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Decision from governance check
    /// </summary>
    public class GovernanceDecision
    {
        public bool IsAllowed { get; set; }
        public string? DenialReason { get; set; }
        public string? Suggestion { get; set; }
        public GovernanceDenialSource? DenialSource { get; set; }
        public Dictionary<string, object> Constraints { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public static GovernanceDecision Allowed(Dictionary<string, object>? constraints = null)
            => new() { IsAllowed = true, Constraints = constraints ?? new() };

        public static GovernanceDecision Denied(string reason, GovernanceDenialSource source, string? suggestion = null)
            => new() 
            { 
                IsAllowed = false, 
                DenialReason = reason, 
                DenialSource = source,
                Suggestion = suggestion 
            };
    }

    /// <summary>
    /// Source of governance denial
    /// </summary>
    public enum GovernanceDenialSource
    {
        CapabilityGate,
        FeatureFlag,
        SafetyRails,
        Custom
    }

    /// <summary>
    /// Default implementation of AI Governance Service
    /// </summary>
    public class AiGovernanceService : IAiGovernanceService
    {
        private readonly ICapabilityGate _capabilityGate;
        private readonly IFeatureFlagService _featureFlagService;
        private readonly ISafetyRails _safetyRails;

        public ICapabilityGate CapabilityGate => _capabilityGate;
        public IFeatureFlagService FeatureFlags => _featureFlagService;
        public ISafetyRails SafetyRails => _safetyRails;

        public AiGovernanceService(
            ICapabilityGate? capabilityGate = null,
            IFeatureFlagService? featureFlagService = null,
            ISafetyRails? safetyRails = null)
        {
            _capabilityGate = capabilityGate ?? new CapabilityGate();
            _featureFlagService = featureFlagService ?? new FeatureFlagService();
            _safetyRails = safetyRails ?? new SafetyRails();
        }

        public async Task<GovernanceDecision> CheckActionAsync(GovernanceRequest request)
        {
            var warnings = new List<string>();

            // 1. SAFETY RAILS FIRST (non-negotiable)
            if (!string.IsNullOrEmpty(request.ContentToValidate))
            {
                var safetyResult = _safetyRails.ValidateContent(request.ContentToValidate, request.ContentType);
                if (!safetyResult.IsSafe)
                {
                    return GovernanceDecision.Denied(
                        safetyResult.Message ?? "Content blocked by safety rails",
                        GovernanceDenialSource.SafetyRails,
                        safetyResult.Suggestion
                    );
                }
            }

            // Check action against safety rails
            var actionCheck = _safetyRails.CheckAction(new SafetyAction
            {
                ActionType = request.ActionType,
                Source = request.Context.RequestingService.ToString(),
                Parameters = request.Parameters,
                Context = request.Context
            });

            if (!actionCheck.IsSafe)
            {
                return GovernanceDecision.Denied(
                    actionCheck.Message ?? "Action blocked by safety rails",
                    GovernanceDenialSource.SafetyRails
                );
            }

            // 2. CAPABILITY GATE
            if (request.RequiredCapability.HasValue)
            {
                var capabilityResult = await _capabilityGate.CheckCapabilityAsync(
                    request.RequiredCapability.Value,
                    request.Context
                );

                if (!capabilityResult.IsAllowed)
                {
                    return GovernanceDecision.Denied(
                        capabilityResult.DenialReason ?? "Capability not allowed",
                        GovernanceDenialSource.CapabilityGate,
                        capabilityResult.AlternativeAction
                    );
                }
            }

            // 3. FEATURE FLAGS
            if (!string.IsNullOrEmpty(request.FeatureFlag))
            {
                var isEnabled = await _featureFlagService.IsEnabledAsync(request.FeatureFlag, request.Context);
                if (!isEnabled)
                {
                    return GovernanceDecision.Denied(
                        $"Feature '{request.FeatureFlag}' is not enabled",
                        GovernanceDenialSource.FeatureFlag,
                        "This feature may be available with a different subscription tier or in a different mode."
                    );
                }
            }

            // All checks passed
            return GovernanceDecision.Allowed();
        }

        public async Task<bool> CanPerformAsync(AiCapability capability, AiPermissionContext context)
        {
            var result = await _capabilityGate.CheckCapabilityAsync(capability, context);
            return result.IsAllowed;
        }

        public async Task<bool> IsFeatureEnabledAsync(string featureKey, AiPermissionContext context)
        {
            return await _featureFlagService.IsEnabledAsync(featureKey, context);
        }

        public SafetyCheckResult ValidateContent(string content, ContentType type)
        {
            return _safetyRails.ValidateContent(content, type);
        }
    }

    /// <summary>
    /// Extension methods for easy governance integration
    /// </summary>
    public static class GovernanceExtensions
    {
        /// <summary>
        /// Execute an action with governance check
        /// </summary>
        public static async Task<T?> WithGovernanceAsync<T>(
            this IAiGovernanceService governance,
            GovernanceRequest request,
            Func<Task<T>> action,
            Func<GovernanceDecision, T>? onDenied = null)
        {
            var decision = await governance.CheckActionAsync(request);
            
            if (!decision.IsAllowed)
            {
                if (onDenied != null)
                {
                    return onDenied(decision);
                }
                return default;
            }

            return await action();
        }

        /// <summary>
        /// Create a permission context for a user
        /// </summary>
        public static AiPermissionContext CreateContext(
            this IAiGovernanceService governance,
            string userId,
            UserTier tier = UserTier.Free,
            GameMode mode = GameMode.Default,
            AiServiceType service = AiServiceType.Unknown)
        {
            return new AiPermissionContext
            {
                UserId = userId,
                Tier = tier,
                Mode = mode,
                RequestingService = service
            };
        }
    }
}
