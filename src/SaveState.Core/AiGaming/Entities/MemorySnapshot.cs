using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

namespace SaveState.Core.AiGaming.Entities;

public class MemorySnapshot : EntityBase
{
    public long Address { get; private set; }
    public byte[] Data { get; private set; } = Array.Empty<byte>();
    public DateTime CapturedAt { get; private set; }
    public string? ProcessName { get; private set; }
    public int ProcessId { get; private set; }

    protected MemorySnapshot() { } // EF Core

    public MemorySnapshot(long address, byte[] data, string? processName, int processId)
    {
        Address = address;
        Data = Guard.Against.Null(data, nameof(data));
        ProcessName = processName;
        ProcessId = processId;
    }

    public static MemorySnapshot Create(long address, byte[] data, string? processName, int processId, ITimeProvider timeProvider)
    {
        return new MemorySnapshot(address, data, processName, processId)
        {
            CapturedAt = timeProvider.UtcNow
        };
    }

    public void UpdateData(byte[] newData)
    {
        Data = Guard.Against.Null(newData, nameof(newData));
    }
}
