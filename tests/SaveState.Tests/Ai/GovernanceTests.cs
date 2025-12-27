using System;
using Xunit;
using SaveState.Core.Services.Ai.Governance;

namespace SaveState.Tests.Ai
{
    /// <summary>
    /// Tests for the AI Governance layer - basic existence verification
    /// </summary>
    public class GovernanceTests
    {
        [Fact]
        public void CapabilityGate_CanBeCreated()
        {
            var gate = new CapabilityGate();
            Assert.NotNull(gate);
        }

        [Fact]
        public void FeatureFlagService_CanBeCreated()
        {
            var service = new FeatureFlagService();
            Assert.NotNull(service);
        }

        [Fact]
        public void SafetyRails_CanBeCreated()
        {
            var rails = new SafetyRails();
            Assert.NotNull(rails);
        }

        [Fact]
        public void AiGovernanceService_CanBeCreated()
        {
            var capabilityGate = new CapabilityGate();
            var featureFlags = new FeatureFlagService();
            var safetyRails = new SafetyRails();
            var service = new AiGovernanceService(capabilityGate, featureFlags, safetyRails);
            Assert.NotNull(service);
        }

        [Fact]
        public void AiPermissionContext_CanBeCreated()
        {
            var context = new AiPermissionContext();
            Assert.NotNull(context);
        }
    }
}
