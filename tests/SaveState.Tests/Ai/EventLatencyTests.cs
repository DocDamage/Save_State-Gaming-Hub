using System;
using Xunit;
using SaveState.Core.Services.Ai.Events;
using SaveState.Core.Services.Ai.Latency;

namespace SaveState.Tests.Ai
{
    /// <summary>
    /// Tests for the Event Bus and Latency layer - basic existence verification
    /// </summary>
    public class EventLatencyTests
    {
        [Fact]
        public void EnhancedEventBus_CanBeCreated()
        {
            var bus = new EnhancedEventBus();
            Assert.NotNull(bus);
        }

        [Fact]
        public void GameEvent_CanBeCreated()
        {
            var evt = new GameEvent();
            Assert.NotNull(evt);
        }

        [Fact]
        public void LatencyManager_CanBeCreated()
        {
            var manager = new LatencyManager();
            Assert.NotNull(manager);
        }

        [Fact]
        public void StreamingHandler_CanBeCreated()
        {
            var handler = new StreamingHandler();
            Assert.NotNull(handler);
        }

        [Fact]
        public void ResponseWarmer_CanBeCreated()
        {
            var warmer = new ResponseWarmer();
            Assert.NotNull(warmer);
        }

        [Fact]
        public void WarmingContext_CanBeCreated()
        {
            var context = new WarmingContext();
            Assert.NotNull(context);
        }
    }
}
