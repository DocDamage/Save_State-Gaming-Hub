using System;
using Xunit;
using SaveState.Core.Services.Ai.Memory;

namespace SaveState.Tests.Ai
{
    /// <summary>
    /// Tests for the Memory layer - basic existence verification
    /// </summary>
    public class MemoryLayerTests
    {
        [Fact]
        public void LoreLocker_CanBeCreated()
        {
            var locker = new LoreLocker();
            Assert.NotNull(locker);
        }

        [Fact]
        public void LockedLore_CanBeCreated()
        {
            var lore = new LockedLore();
            Assert.NotNull(lore);
        }

        [Fact]
        public void NarrativeCompressor_CanBeCreated()
        {
            var compressor = new NarrativeCompressor();
            Assert.NotNull(compressor);
        }

        [Fact]
        public void NarrativeInput_CanBeCreated()
        {
            var input = new NarrativeInput();
            Assert.NotNull(input);
        }

        [Fact]
        public void NarrativeEvent_CanBeCreated()
        {
            var evt = new NarrativeEvent();
            Assert.NotNull(evt);
        }

        [Fact]
        public void CompressionStatistics_CanBeCreated()
        {
            var stats = new CompressionStatistics();
            Assert.NotNull(stats);
        }
    }
}
