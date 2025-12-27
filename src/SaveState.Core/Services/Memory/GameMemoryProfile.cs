using System;
using System.Collections.Generic;

namespace SaveState.Core.Services.Memory
{
    public class GameMemoryProfile
    {
        public Guid GameId { get; set; }
        public string GameTitle { get; set; } = string.Empty;
        public Dictionary<string, MemoryValueDefinition> MemoryMap { get; set; } = new();
    }

    public class MemoryValueDefinition
    {
        /// <summary>
        /// Base address in Hex string (e.g. "0x0040A230") or relative to module (e.g. "game.exe+0xA230")
        /// </summary>
        public string BaseAddress { get; set; } = string.Empty;

        /// <summary>
        /// Optional pointer offsets
        /// </summary>
        public int[]? Offsets { get; set; }

        public MemoryValueType Type { get; set; } = MemoryValueType.Int;
    }

    public enum MemoryValueType
    {
        Int,
        Float,
        String,
        Byte
    }
}
