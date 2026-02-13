using SaveState.Core.Common;
using SaveState.Core.Performance.ValueObjects;

namespace SaveState.Core.Performance.Services;

/// <summary>
/// Service for reading process memory.
/// </summary>
public interface IMemoryReader
{
    /// <summary>
    /// Reads raw bytes from a process memory address.
    /// </summary>
    /// <param name="processId">The target process ID.</param>
    /// <param name="address">The memory address to read from.</param>
    /// <param name="size">Number of bytes to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the raw bytes or error.</returns>
    Task<Result<byte[]>> ReadMemoryAsync(
        int processId,
        MemoryAddress address,
        int size,
        CancellationToken ct = default);

    /// <summary>
    /// Reads a typed value from memory.
    /// </summary>
    /// <typeparam name="T">The type to read (int, float, etc.).</typeparam>
    /// <param name="processId">The target process ID.</param>
    /// <param name="address">The memory address to read from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the typed value or error.</returns>
    Task<Result<T>> ReadValueAsync<T>(
        int processId,
        MemoryAddress address,
        CancellationToken ct = default) where T : struct;

    /// <summary>
    /// Resolves a pointer chain to get the final address.
    /// </summary>
    /// <param name="processId">The target process ID.</param>
    /// <param name="address">The pointer chain address.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the resolved address or error.</returns>
    Task<Result<long>> ResolvePointerChainAsync(
        int processId,
        MemoryAddress address,
        CancellationToken ct = default);

    /// <summary>
    /// Writes raw bytes to a process memory address.
    /// </summary>
    /// <param name="processId">The target process ID.</param>
    /// <param name="address">The memory address to write to.</param>
    /// <param name="buffer">The bytes to write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or error.</returns>
    Task<Result> WriteMemoryAsync(
        int processId,
        MemoryAddress address,
        byte[] buffer,
        CancellationToken ct = default);

    /// <summary>
    /// Writes a typed value to memory.
    /// </summary>
    /// <typeparam name="T">The type to write.</typeparam>
    /// <param name="processId">The target process ID.</param>
    /// <param name="address">The memory address to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or error.</returns>
    Task<Result> WriteValueAsync<T>(
        int processId,
        MemoryAddress address,
        T value,
        CancellationToken ct = default) where T : struct;

    /// <summary>
    /// Checks if a process is accessible for memory reading.
    /// </summary>
    /// <param name="processId">The process ID to check.</param>
    /// <returns>Result indicating success or access error.</returns>
    Result<bool> CanAccessProcess(int processId);

    /// <summary>
    /// Gets the base address of a module in the target process.
    /// </summary>
    /// <param name="processId">The target process ID.</param>
    /// <param name="moduleName">The module name (e.g., "game.exe").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the module base address or error.</returns>
    Task<Result<long>> GetModuleBaseAddressAsync(
        int processId,
        string moduleName,
        CancellationToken ct = default);
}
