using SaveState.Core.Common;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.GameLibrary;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.RomManagement.Services;

public class EmulatorService : IEmulatorService
{
    private readonly IEmulatorRepository _emulatorRepository;
    private readonly IRomFileRepository _romFileRepository;
    private readonly IPlatformRepository _platformRepository;
    private readonly ILogger<EmulatorService> _logger;

    // Track running emulator processes: RomFileId -> (ProcessId, StartTime)
    private readonly Dictionary<Guid, (int ProcessId, DateTime StartTime)> _runningProcesses = new();

    public EmulatorService(
        IEmulatorRepository emulatorRepository,
        IRomFileRepository romFileRepository,
        IPlatformRepository platformRepository,
        ILogger<EmulatorService> logger)
    {
        _emulatorRepository = emulatorRepository ?? throw new ArgumentNullException(nameof(emulatorRepository));
        _romFileRepository = romFileRepository ?? throw new ArgumentNullException(nameof(romFileRepository));
        _platformRepository = platformRepository ?? throw new ArgumentNullException(nameof(platformRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EmulatorLaunchResult> LaunchRomAsync(Guid romFileId, CancellationToken ct = default)
    {
        var romFile = await _romFileRepository.GetByIdAsync(romFileId, ct).ConfigureAwait(false);
        if (romFile == null)
        {
            _logger.LogWarning("ROM file not found: {RomFileId}", romFileId);
            return new EmulatorLaunchResult(false, "ROM file not found", null, null);
        }

        var emulator = await _emulatorRepository.GetByPlatformIdAsync(romFile.PlatformId, ct).ConfigureAwait(false);
        if (emulator == null)
        {
            _logger.LogWarning("No emulator found for platform: {PlatformId}", romFile.PlatformId);
            return new EmulatorLaunchResult(false, "No emulator configured for this platform", null, null);
        }

        return await LaunchRomWithEmulatorAsync(romFileId, emulator.Id, ct).ConfigureAwait(false);
    }

    public async Task<EmulatorLaunchResult> LaunchRomWithEmulatorAsync(Guid romFileId, Guid emulatorId, CancellationToken ct = default)
    {
        var romFile = await _romFileRepository.GetByIdAsync(romFileId, ct).ConfigureAwait(false);
        if (romFile == null)
        {
            return new EmulatorLaunchResult(false, "ROM file not found", null, null);
        }

        var emulator = await _emulatorRepository.GetByIdAsync(emulatorId, ct).ConfigureAwait(false);
        if (emulator == null)
        {
            return new EmulatorLaunchResult(false, "Emulator not found", null, null);
        }

        // Check if emulator executable exists
        if (!File.Exists(emulator.ExecutablePath.Value))
        {
            return new EmulatorLaunchResult(false, "Emulator executable not found", null, null);
        }

        try
        {
            // Kill any existing process for this ROM
            await KillEmulatorProcessAsync(romFileId, CancellationToken.None).ConfigureAwait(false);

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = emulator.ExecutablePath.Value,
                Arguments = BuildArguments(emulator.CommandLineArgs, romFile.FilePath.Value),
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(emulator.ExecutablePath.Value) ?? string.Empty
            };

            var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start emulator process for ROM: {RomFileId}", romFileId);
                return new EmulatorLaunchResult(false, "Failed to start emulator process", null, emulatorId);
            }

            // Track the running process
            _runningProcesses[romFileId] = (process.Id, DateTime.UtcNow);

            _logger.LogInformation("Successfully launched ROM {RomFileId} with emulator {EmulatorId} (Process: {ProcessId})",
                romFileId, emulatorId, process.Id);

            return new EmulatorLaunchResult(true, null, process.Id, emulatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error launching ROM {RomFileId} with emulator {EmulatorId}", romFileId, emulatorId);
            return new EmulatorLaunchResult(false, $"Launch failed: {ex.Message}", null, emulatorId);
        }
    }

    public async Task<IReadOnlyList<EmulatorInfo>> GetAvailableEmulatorsAsync(Guid platformId, CancellationToken ct = default)
    {
        var targetPlatform = await _platformRepository.GetByIdAsync(platformId, ct).ConfigureAwait(false);
        if (targetPlatform == null)
        {
            _logger.LogWarning("Platform not found: {PlatformId}", platformId);
            return Array.Empty<EmulatorInfo>();
        }

        var allEmulators = await _emulatorRepository.GetAllAsync(ct).ConfigureAwait(false);

        // Get emulators for the exact platform first
        var exactPlatformEmulators = allEmulators
            .Where(e => e.PlatformId == platformId && File.Exists(e.ExecutablePath.Value));

        // Get emulators for platforms of the same type (compatible emulators)
        var compatibleEmulators = allEmulators
            .Where(e => e.PlatformId != platformId &&
                       e.Platform?.Type == targetPlatform.Type &&
                       File.Exists(e.ExecutablePath.Value));

        return exactPlatformEmulators
            .Concat(compatibleEmulators)
            .Select(e => new EmulatorInfo(
                e.Id,
                e.Name,
                e.ExecutablePath.Value,
                e.Version,
                e.Description,
                true)) // Since we checked File.Exists above
            .ToList();
    }

    public async Task<Result<EmulatorInfo>> GetDefaultEmulatorAsync(Guid platformId, CancellationToken ct = default)
    {
        var emulator = await _emulatorRepository.GetByPlatformIdAsync(platformId, ct).ConfigureAwait(false);
        if (emulator == null)
            return Result.Failure<EmulatorInfo>("No default emulator configured for this platform", ErrorType.NotFound);

        return Result.Success<EmulatorInfo>(new EmulatorInfo(
            emulator.Id,
            emulator.Name,
            emulator.ExecutablePath.Value,
            null, // emulator.Version
            null, // emulator.Description
            File.Exists(emulator.ExecutablePath.Value)));
    }

    public async Task<bool> IsEmulatorAvailableAsync(Guid emulatorId, CancellationToken ct = default)
    {
        var emulator = await _emulatorRepository.GetByIdAsync(emulatorId, ct).ConfigureAwait(false);
        return emulator != null && File.Exists(emulator.ExecutablePath.Value);
    }

    public Task<Result<ProcessInfo>> GetRunningEmulatorProcessAsync(Guid romFileId, CancellationToken ct = default)
    {
        if (!_runningProcesses.TryGetValue(romFileId, out var processInfo))
        {
            return Task.FromResult(Result.Failure<ProcessInfo>("No emulator process running for this ROM", ErrorType.NotFound));
        }

        try
        {
            var process = System.Diagnostics.Process.GetProcessById(processInfo.ProcessId);
            if (!process.HasExited)
            {
                return Task.FromResult(Result.Success<ProcessInfo>(new ProcessInfo(
                    process.Id,
                    process.ProcessName,
                    processInfo.StartTime,
                    process.WorkingSet64)));
            }
            else
            {
                // Process has exited, clean up tracking
                _runningProcesses.Remove(romFileId);
                return Task.FromResult(Result.Failure<ProcessInfo>("Emulator process has exited", ErrorType.NotFound));
            }
        }
        catch (ArgumentException)
        {
            // Process doesn't exist anymore
            _runningProcesses.Remove(romFileId);
            return Task.FromResult(Result.Failure<ProcessInfo>("Emulator process no longer exists", ErrorType.NotFound));
        }
    }

    public Task KillEmulatorProcessAsync(Guid romFileId, CancellationToken ct = default)
    {
        if (!_runningProcesses.TryGetValue(romFileId, out var processInfo))
        {
            return Task.CompletedTask;
        }

        try
        {
            var process = System.Diagnostics.Process.GetProcessById(processInfo.ProcessId);
            if (!process.HasExited)
            {
                _logger.LogInformation("Killing emulator process {ProcessId} for ROM {RomFileId}",
                    processInfo.ProcessId, romFileId);

                process.Kill();
                process.WaitForExit(5000); // Wait up to 5 seconds
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error killing emulator process for ROM {RomFileId}", romFileId);
        }
        finally
        {
            _runningProcesses.Remove(romFileId);
        }
        return Task.CompletedTask;
    }

    private static string BuildArguments(string? commandLineArgs, string romPath)
    {
        if (string.IsNullOrWhiteSpace(commandLineArgs))
        {
            // Default: just pass the ROM path
            return $"\"{romPath}\"";
        }

        // Replace {ROM} placeholder with actual ROM path
        var args = commandLineArgs.Replace("{ROM}", $"\"{romPath}\"");
        return args;
    }
}

