using Ardalis.GuardClauses;
using SaveState.Core.Common.Base;

namespace SaveState.Core.Performance.ValueObjects;

/// <summary>
/// Represents a memory address in a process with type safety and validation.
/// </summary>
public sealed class MemoryAddress : ValueObject
{
    /// <summary>
    /// Gets the base address (absolute or module-relative).
    /// </summary>
    public long BaseAddress { get; }

    /// <summary>
    /// Gets the optional offsets for pointer chains.
    /// </summary>
    public IReadOnlyList<int> Offsets { get; }

    /// <summary>
    /// Gets whether this is a pointer chain (has offsets).
    /// </summary>
    public bool IsPointerChain => Offsets.Count > 0;

    /// <summary>
    /// Gets the final calculated address (base + offsets resolved).
    /// </summary>
    public long FinalAddress { get; private set; }

    private MemoryAddress(long baseAddress, IReadOnlyList<int> offsets)
    {
        BaseAddress = Guard.Against.Negative(baseAddress, nameof(baseAddress));
        Offsets = offsets ?? Array.Empty<int>();
        FinalAddress = baseAddress;
    }

    /// <summary>
    /// Creates a simple memory address without offsets.
    /// </summary>
    public static MemoryAddress Create(long address)
    {
        return new MemoryAddress(address, Array.Empty<int>());
    }

    /// <summary>
    /// Creates a pointer chain address with base + offsets.
    /// </summary>
    public static MemoryAddress CreatePointerChain(long baseAddress, params int[] offsets)
    {
        Guard.Against.Null(offsets, nameof(offsets));
        return new MemoryAddress(baseAddress, offsets);
    }

    /// <summary>
    /// Updates the final address after resolving pointer chain.
    /// </summary>
    public void UpdateFinalAddress(long resolvedAddress)
    {
        FinalAddress = resolvedAddress;
    }

    /// <summary>
    /// Formats the address as a hexadecimal string.
    /// </summary>
    public string ToHexString()
    {
        if (IsPointerChain)
        {
            var offsetStr = string.Join(" + ", Offsets.Select(o => $"0x{o:X}"));
            return $"0x{BaseAddress:X} + {offsetStr} → 0x{FinalAddress:X}";
        }
        return $"0x{FinalAddress:X}";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BaseAddress;
        foreach (var offset in Offsets)
        {
            yield return offset;
        }
    }

    public override string ToString() => ToHexString();
}
