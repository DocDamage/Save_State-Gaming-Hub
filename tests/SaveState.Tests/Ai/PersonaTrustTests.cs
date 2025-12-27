using System;
using Xunit;
using SaveState.Core.Services.Ai.Persona;
using SaveState.Core.Services.Ai.Trust;

namespace SaveState.Tests.Ai
{
    /// <summary>
    /// Tests for Persona and Trust layers - basic existence verification
    /// </summary>
    public class PersonaTrustTests
    {
        [Fact]
        public void PersonaHotSwapper_CanBeCreated()
        {
            var swapper = new PersonaHotSwapper();
            Assert.NotNull(swapper);
        }

        [Fact]
        public void PersonaState_CanBeCreated()
        {
            var state = new PersonaState();
            Assert.NotNull(state);
        }

        [Fact]
        public void PlayerTrustModel_CanBeCreated()
        {
            var model = new PlayerTrustModel();
            Assert.NotNull(model);
        }

        [Fact]
        public void TrustProfile_CanBeCreated()
        {
            var profile = new TrustProfile();
            Assert.NotNull(profile);
        }
    }
}
