using System;
using Xunit;
using SaveState.Core.Services.Ai.Tools;
using SaveState.Core.Services.Ai.Resilience;

namespace SaveState.Tests.Ai
{
    /// <summary>
    /// Tests for Tools and Resilience layers - basic existence verification
    /// </summary>
    public class ToolsResilienceTests
    {
        [Fact]
        public void ToolAwareAi_CanBeCreated()
        {
            var ai = new ToolAwareAi();
            Assert.NotNull(ai);
        }

        [Fact]
        public void FailureAsContent_CanBeCreated()
        {
            var handler = new FailureAsContent();
            Assert.NotNull(handler);
        }

        [Fact]
        public void AiFailure_CanBeCreated()
        {
            var failure = new AiFailure();
            Assert.NotNull(failure);
        }

        [Fact]
        public void NarrativeFailure_CanBeCreated()
        {
            var narrative = new NarrativeFailure();
            Assert.NotNull(narrative);
        }
    }
}
