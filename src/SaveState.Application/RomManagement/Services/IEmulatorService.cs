using SaveState.Core.Common;
using SaveState.Core.RomManagement.Entities;

namespace SaveState.Application.RomManagement.Services;

public interface IEmulatorService
{
    Task<EmulatorLaunchResult> LaunchRomAsync(Guid romFileId, CancellationToken ct = default);
    Task<EmulatorLaunchResult> LaunchRomWithEmulatorAsync(Guid romFileId, Guid emulatorId, CancellationToken ct = default);
    Task<IReadOnlyList<EmulatorInfo>> GetAvailableEmulatorsAsync(Guid platformId, CancellationToken ct = default);
    Task<Result<EmulatorInfo>> GetDefaultEmulatorAsync(Guid platformId, CancellationToken ct = default);
    Task<bool> IsEmulatorAvailableAsync(Guid emulatorId, CancellationToken ct = default);
    Task<Result<ProcessInfo>> GetRunningEmulatorProcessAsync(Guid romFileId, CancellationToken ct = default);
    Task KillEmulatorProcessAsync(Guid romFileId, CancellationToken ct = default);
}

public record EmulatorInfo(
    Guid Id,
    string Name,
    string ExecutablePath,
    string? Version,
    string? Description,
    bool IsAvailable);

public record EmulatorLaunchResult(
    bool Success,
    string? ErrorMessage,
    int? ProcessId,
    Guid? EmulatorId);

public record ProcessInfo(
    int Id,
    string ProcessName,
    DateTime StartTime,
    long MemoryUsage);
