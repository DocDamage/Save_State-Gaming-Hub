using System;
using Xunit;
using SaveState.Core.Services.Ai.Core;

namespace SaveState.Tests.Ai
{
    /// <summary>
    /// Tests for the Deterministic Core layer - basic existence verification
    /// </summary>
    public class CoreLayerTests
    {
        [Fact]
        public void DeterministicBoundary_CanBeCreated()
        {
            var boundary = new DeterministicBoundary();
            Assert.NotNull(boundary);
        }

        [Fact]
        public void CanonEnforcer_CanBeCreated()
        {
            var enforcer = new CanonEnforcer();
            Assert.NotNull(enforcer);
        }

        [Fact]
        public void StateIntegrity_CanBeCreated()
        {
            var integrity = new StateIntegrity();
            Assert.NotNull(integrity);
        }

        [Fact]
        public void StateModification_CanBeCreated()
        {
            var mod = new StateModification();
            Assert.NotNull(mod);
        }

        [Fact]
        public void CanonicalFact_CanBeCreated()
        {
            var fact = new CanonicalFact();
            Assert.NotNull(fact);
        }
    }
}
