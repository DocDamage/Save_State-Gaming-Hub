using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

public class GameMemoryReader : IGameMemoryReader, IDisposable
{
    private readonly ILogger<GameMemoryReader> _logger;
    private readonly MemoryPatternDatabase _patternDatabase;
    private IntPtr _processHandle = IntPtr.Zero;
    private Process? _attachedProcess;
    private bool _isAttached;
    private readonly Timer _monitoringTimer;
    private readonly List<MemoryPattern> _knownPatterns = new();
    private GameStateType _currentState = GameStateType.Unknown;
    private string? _gameTitle;

    public event EventHandler<GameStateChangedEventArgs>? StateChanged;

    public bool IsAttached => _isAttached;

    public GameMemoryReader(ILogger<GameMemoryReader> logger, MemoryPatternDatabase patternDatabase)
    {
        _logger = logger;
        _patternDatabase = patternDatabase;

        // Monitor game state every 2 seconds
        _monitoringTimer = new Timer(MonitorGameState, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task<Result> AttachToProcessAsync(int processId, CancellationToken ct = default)
    {
        try
        {
            if (_isAttached)
            {
                return Task.FromResult(Result.Failure("Already attached to a process. Detach first."));
            }

            _logger.LogInformation("Attempting to attach to process {ProcessId}", processId);

            // Get the process
            _attachedProcess = Process.GetProcessById(processId);
            if (_attachedProcess == null)
            {
                return Task.FromResult(Result.Failure($"Process {processId} not found"));
            }

            // Open process with read access
            _processHandle = OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ, false, processId);
            if (_processHandle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogError("Failed to open process {ProcessId}: Win32 error {Error}", processId, error);
                return Task.FromResult(Result.Failure($"Failed to open process for memory reading (Win32 error: {error})"));
            }

            _isAttached = true;

            // Try to identify the game from process name
            _gameTitle = IdentifyGameFromProcess(_attachedProcess);

            _logger.LogInformation("Successfully attached to process {ProcessId} ({ProcessName}), identified as game '{Game}'",
                processId, _attachedProcess.ProcessName, _gameTitle ?? "Unknown");

            // Start monitoring
            _monitoringTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(2));

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching to process {ProcessId}", processId);
            return Task.FromResult(Result.Failure($"Failed to attach to process: {ex.Message}"));
        }
    }

    public Task<Result> DetachAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_isAttached)
            {
                return Task.FromResult(Result.Success()); // Already detached
            }

            _logger.LogInformation("Detaching from process");

            // Stop monitoring
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // Close process handle
            if (_processHandle != IntPtr.Zero)
            {
                CloseHandle(_processHandle);
                _processHandle = IntPtr.Zero;
            }

            _attachedProcess = null;
            _isAttached = false;
            _currentState = GameStateType.Unknown;

            _logger.LogInformation("Successfully detached from process");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching from process");
            return Task.FromResult(Result.Failure($"Failed to detach from process: {ex.Message}"));
        }
    }

    public async Task<Result<IReadOnlyList<MemoryPattern>>> DetectPatternsAsync(CancellationToken ct = default)
    {
        if (!_isAttached)
        {
            return Result<IReadOnlyList<MemoryPattern>>.Failure("Not attached to any process");
        }

        try
        {
            var patterns = new List<MemoryPattern>();

            if (_gameTitle != null)
            {
                // Use known signatures for this game
                var signaturesResult = _patternDatabase.GetSignaturesForGame(_gameTitle);
                if (signaturesResult.IsSuccess && signaturesResult.Value.Any())
                {
                    foreach (var signature in signaturesResult.Value)
                    {
                        ct.ThrowIfCancellationRequested();

                        var patternResult = await ScanForSignatureAsync(signature, ct);
                        if (patternResult.IsSuccess)
                        {
                            patterns.Add(patternResult.Value!);
                        }
                    }
                }
            }

            // Fallback: Scan for common patterns if no known signatures found
            if (patterns.Count == 0)
            {
                _logger.LogInformation("No known signatures found for game '{Game}', scanning for common patterns", _gameTitle);

                // Example: Look for health values (common pattern)
                var healthPatternResult = await ScanForHealthValueAsync(ct);
                if (healthPatternResult.IsSuccess)
                {
                    patterns.Add(healthPatternResult.Value!);
                }

                // Example: Look for score/level values
                var scorePatternResult = await ScanForScoreValueAsync(ct);
                if (scorePatternResult.IsSuccess)
                {
                    patterns.Add(scorePatternResult.Value!);
                }
            }

            _logger.LogInformation("Detected {Count} memory patterns", patterns.Count);
            return Result<IReadOnlyList<MemoryPattern>>.Success(patterns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting memory patterns");
            return Result<IReadOnlyList<MemoryPattern>>.Failure($"Failed to detect patterns: {ex.Message}");
        }
    }

    private async Task<Result<MemoryPattern>> ScanForHealthValueAsync(CancellationToken ct)
    {
        // Simplified health detection - look for values between 0-100 in common memory ranges
        // In a real implementation, this would use signature scanning

        const int maxHealth = 100;
        var scanRanges = new[]
        {
            new { Start = 0x00400000, Size = 0x100000 }, // Common game memory range
            new { Start = 0x10000000, Size = 0x100000 }  // Another common range
        };

        foreach (var range in scanRanges)
        {
            ct.ThrowIfCancellationRequested();

            for (long offset = 0; offset < range.Size; offset += 4)
            {
                var address = (IntPtr)(range.Start + offset);
                var value = ReadInt32(address);

                if (value.HasValue && value.Value > 0 && value.Value <= maxHealth)
                {
                    // Verify it's actually health by checking if it changes
                    await Task.Delay(100, ct);
                    var newValue = ReadInt32(address);

                    if (newValue.HasValue && newValue != value)
                    {
                        return Result<MemoryPattern>.Success(new MemoryPattern("Health", address, "int32", newValue.Value));
                    }
                }
            }
        }

        return Result<MemoryPattern>.Failure("Health pattern not found", ErrorType.NotFound);
    }

    private async Task<Result<MemoryPattern>> ScanForScoreValueAsync(CancellationToken ct)
    {
        // Look for increasing score values
        const int maxScore = 999999;
        var scanRanges = new[]
        {
            new { Start = 0x00400000, Size = 0x100000 },
            new { Start = 0x10000000, Size = 0x100000 }
        };

        foreach (var range in scanRanges)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Yield(); // Ensure method is async and responsive to cancellation

            for (long offset = 0; offset < range.Size; offset += 4)
            {
                var address = (IntPtr)(range.Start + offset);
                var value = ReadInt32(address);

                if (value.HasValue && value.Value > 0 && value.Value <= maxScore)
                {
                    return Result<MemoryPattern>.Success(new MemoryPattern("Score", address, "int32", value.Value));
                }
            }
        }

        return Result<MemoryPattern>.Failure("Score pattern not found", ErrorType.NotFound);
    }

    private void MonitorGameState(object? state)
    {
        if (!_isAttached || _attachedProcess == null)
            return;

        try
        {
            var newState = DetectCurrentGameState();
            if (newState != _currentState)
            {
                _currentState = newState;
                _logger.LogInformation("Game state changed to {State}", newState);

                StateChanged?.Invoke(this, new GameStateChangedEventArgs
                {
                    StateType = newState,
                    Data = GetStateData(newState)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring game state");
        }
    }

    private GameStateType DetectCurrentGameState()
    {
        // Simplified state detection based on memory patterns
        // In practice, this would use the detected patterns to determine state

        if (!_isAttached)
            return GameStateType.Unknown;

        // Check if process is responding (basic liveness check)
        try
        {
            return _attachedProcess?.Responding == true
                ? GameStateType.InGame
                : GameStateType.Unknown;
        }
        catch
        {
            return GameStateType.Unknown;
        }
    }

    private object? GetStateData(GameStateType state)
    {
        // Return state-specific data
        return state switch
        {
            GameStateType.InGame => new { IsActive = true },
            GameStateType.Paused => new { IsPaused = true },
            _ => null
        };
    }

    private int? ReadInt32(IntPtr address)
    {
        if (!_isAttached || _processHandle == IntPtr.Zero)
            return null;

        var buffer = new byte[4];
        uint bytesRead;

        if (ReadProcessMemory(_processHandle, address, buffer, 4, out bytesRead) && bytesRead == 4)
        {
            return BitConverter.ToInt32(buffer, 0);
        }

        return null;
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        DetachAsync().GetAwaiter().GetResult();
    }

    // Windows API declarations
    [Flags]
    private enum PROCESS_ACCESS_RIGHTS : uint
    {
        PROCESS_VM_READ = 0x0010
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(PROCESS_ACCESS_RIGHTS dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out uint lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private string? IdentifyGameFromProcess(Process process)
    {
        // Map process names to game titles
        var processName = Path.GetFileNameWithoutExtension(process.ProcessName).ToLowerInvariant();

        return processName switch
        {
            "celeste" => "Celeste",
            "hollowknight" => "Hollow Knight",
            "stardew valley" or "stardewvalley" => "Stardew Valley",
            "celeste.bin" or "celeste.exe" => "Celeste", // Handle different executable names
            _ => null // Unknown game
        };
    }

    private Task<Result<MemoryPattern>> ScanForSignatureAsync(GameMemorySignature signature, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            try
            {
                // Convert hex pattern to byte array
                var patternBytes = HexStringToByteArray(signature.Pattern);
                if (patternBytes == null || patternBytes.Length == 0)
                {
                    return Result<MemoryPattern>.Failure($"Invalid hex pattern: {signature.Pattern}", ErrorType.Validation);
                }

                // Scan memory for the pattern
                var address = ScanForPattern(patternBytes, ct);
                if (address == IntPtr.Zero)
                {
                    return Result<MemoryPattern>.Failure($"Signature '{signature.Name}' not found", ErrorType.NotFound);
                }

                // Apply offset and read value
                var valueAddress = IntPtr.Add(address, signature.Offset);
                var value = ReadValue(valueAddress, signature.ValueType);

                if (value != null)
                {
                    return Result<MemoryPattern>.Success(new MemoryPattern(signature.Name, valueAddress, signature.ValueType, value));
                }

                return Result<MemoryPattern>.Failure($"Value at offset for signature '{signature.Name}' could not be read", ErrorType.Internal);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scanning for signature '{Signature}'", signature.Name);
                return Result<MemoryPattern>.Failure($"Error scanning for signature: {ex.Message}", ErrorType.Internal);
            }
        }, ct);
    }

    private IntPtr ScanForPattern(byte[] pattern, CancellationToken ct)
    {
        // Simplified pattern scanning - scan common memory ranges
        var scanRanges = new[]
        {
            new { Start = 0x00400000, Size = 0x100000 }, // Common game memory range
            new { Start = 0x10000000, Size = 0x100000 }  // Another common range
        };

        foreach (var range in scanRanges)
        {
            ct.ThrowIfCancellationRequested();

            for (long offset = 0; offset < range.Size - pattern.Length; offset++)
            {
                var address = (IntPtr)(range.Start + offset);

                if (MatchesPattern(address, pattern))
                {
                    return address;
                }
            }
        }

        return IntPtr.Zero;
    }

    private bool MatchesPattern(IntPtr address, byte[] pattern)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            var byteValue = ReadByte(IntPtr.Add(address, i));
            if (!byteValue.HasValue || byteValue.Value != pattern[i])
            {
                return false;
            }
        }
        return true;
    }

    private byte? ReadByte(IntPtr address)
    {
        if (!_isAttached || _processHandle == IntPtr.Zero)
            return null;

        var buffer = new byte[1];
        uint bytesRead;

        if (ReadProcessMemory(_processHandle, address, buffer, 1, out bytesRead) && bytesRead == 1)
        {
            return buffer[0];
        }

        return null;
    }

    private object? ReadValue(IntPtr address, string valueType)
    {
        return valueType.ToLowerInvariant() switch
        {
            "int32" => ReadInt32(address),
            "float" => ReadFloat(address),
            "byte" => ReadByte(address),
            _ => ReadInt32(address) // Default to int32
        };
    }

    private float? ReadFloat(IntPtr address)
    {
        if (!_isAttached || _processHandle == IntPtr.Zero)
            return null;

        var buffer = new byte[4];
        uint bytesRead;

        if (ReadProcessMemory(_processHandle, address, buffer, 4, out bytesRead) && bytesRead == 4)
        {
            return BitConverter.ToSingle(buffer, 0);
        }

        return null;
    }

    private static byte[]? HexStringToByteArray(string hex)
    {
        try
        {
            // Remove spaces and handle wildcards (??)
            hex = hex.Replace(" ", "").Replace("??", "00"); // Treat ?? as 00 for simplicity

            if (hex.Length % 2 != 0)
                return null;

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
        catch
        {
            return null;
        }
    }
}
