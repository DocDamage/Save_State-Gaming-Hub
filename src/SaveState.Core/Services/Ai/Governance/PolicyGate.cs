using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Governance.Models;

namespace SaveState.Core.Services.Ai.Governance
{
    public interface IPolicyGate
    {
        /// <summary>
        /// Checks if a request complies with the active AI Contract.
        /// </summary>
        Task<GovernanceDecision> EnforceContractAsync(AiContract contract, GovernanceRequest request);

        /// <summary>
        /// Specifically checks if a tool call is permitted by the contract.
        /// </summary>
        Task<GovernanceDecision> CheckToolCallAsync(AiContract contract, string toolName);
    }

    public class PolicyGate : IPolicyGate
    {
        public Task<GovernanceDecision> EnforceContractAsync(AiContract contract, GovernanceRequest request)
        {
            // 1. Check Capability
            if (request.RequiredCapability.HasValue)
            {
                if (!contract.AllowedCapabilities.Contains(request.RequiredCapability.Value))
                {
                    return Task.FromResult(GovernanceDecision.Denied(
                        $"Capability '{request.RequiredCapability}' is not allowed by contract '{contract.Name}' (ID: {contract.ContractId})",
                        GovernanceDenialSource.PolicyGate // We might need to add this enum value
                    ));
                }
            }

            // 2. Check Action Type specific logic (if any)
            // For now, we rely mainly on Capabilities and Tools

            // 3. Check for specific prohibited content types for this contract, if we had that detail in GovernanceRequest
            // (e.g. if contract says PersistancePolicy.NoPersist but request is "WriteToMemory")

            return Task.FromResult(GovernanceDecision.Allowed());
        }

        public Task<GovernanceDecision> CheckToolCallAsync(AiContract contract, string toolName)
        {
            if (contract.AllowedTools.Contains("*"))
            {
                // Wildcard allows all, but checks for exclusions (e.g. "!system_core")
                // Simple implementation: check if explicit exclusion exists
                 // Note: The model I wrote just has HashSet<string>, so I need to interpret it.
                 // If I put "!tool" in the set, it means excluded.
                 
                 // Realistically, for HashSet, if it contains "*", we assume yes unless strict deny list exists.
                 // My model doesn't have a separate deny list, so I'll assume standard wildcard behavior for now.
                 // A better implementation would parse the allowed list properly.
                 
                 // Let's iterate to check for negations
                 bool explicitlyExcluded = contract.AllowedTools.Any(t => t.StartsWith("!") && t.Substring(1) == toolName);
                 if (explicitlyExcluded)
                 {
                     return Task.FromResult(GovernanceDecision.Denied(
                        $"Tool '{toolName}' is explicitly forbidden by contract '{contract.Name}'",
                        GovernanceDenialSource.PolicyGate
                    ));
                 }
                 
                 return Task.FromResult(GovernanceDecision.Allowed());
            }

            if (contract.AllowedTools.Contains(toolName))
            {
                return Task.FromResult(GovernanceDecision.Allowed());
            }

            return Task.FromResult(GovernanceDecision.Denied(
                $"Tool '{toolName}' is not permitted by contract '{contract.Name}'",
                GovernanceDenialSource.PolicyGate
            ));
        }
    }
}
