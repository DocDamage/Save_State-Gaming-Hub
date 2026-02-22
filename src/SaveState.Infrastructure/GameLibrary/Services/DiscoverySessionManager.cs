using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;
using static SaveState.Infrastructure.Logging.CorrelationIdExtensions;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Manages discovery session lifecycle - creating, tracking, and stopping sessions.
/// </summary>
public sealed class DiscoverySessionManager : IDisposable
{
    private readonly ILogger<DiscoverySessionManager> _logger;
    private readonly Dictionary<Guid, DiscoverySessionContext> _activeSessions = new();
    private readonly object _sessionLock = new();

    // Windows API for process access
    [Flags]
    private enum ProcessAccessRights : uint
    {
        ProcessVmRead = 0x0010,
        ProcessVmWrite = 0x0020,
        ProcessVmOperation = 0x0008,
        ProcessQueryInformation = 0x0400
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(ProcessAccessRights dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    public DiscoverySessionManager(ILogger<DiscoverySessionManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Starts a new discovery session for the specified process.
    /// </summary>
    public Task<Result<DiscoverySession>> StartSessionAsync(int processId, DiscoveryOptions options, CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid();
        
        using (_logger.BeginCorrelationScope(sessionId.ToString("N")))
        using (_logger.BeginSessionScope(sessionId))
        {
            _logger.LogInformation(
                "Starting discovery session {SessionId} for process {ProcessId}. ScanRange: {StartAddress:X}-{EndAddress:X}",
                sessionId,
                processId,
                options.ScanStartAddress,
                options.ScanStartAddress + options.ScanSize);
                
            try
            {
                // Validate process exists
                Process? process = null;
                try
                {
                    process = Process.GetProcessById(processId);
                }
                catch (ArgumentException)
                {
                    _logger.LogError("Process {ProcessId} not found", processId);
                    return Task.FromResult(Result.Failure<DiscoverySession>($"Process {processId} not found", ErrorType.NotFound));
                }

                // Open process handle
                var processHandle = OpenProcess(ProcessAccessRights.ProcessVmRead, false, processId);
                if (processHandle == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    _logger.LogError(
                        "Failed to start discovery session {SessionId}: Win32 error {Error}", 
                        sessionId, 
                        error);
                    return Task.FromResult(Result.Failure<DiscoverySession>(
                        $"Failed to open process for memory reading (Win32 error: {error})", ErrorType.External));
                }

                // Create session
                var session = new DiscoverySession
                {
                    ProcessId = processId,
                    Options = options,
                    IsActive = true,
                    CurrentPass = 0
                };

                var context = new DiscoverySessionContext
                {
                    Session = session,
                    ProcessHandle = processHandle,
                    Process = process
                };

                lock (_sessionLock)
                {
                    _activeSessions[session.SessionId] = context;
                }

                _logger.LogInformation(
                    "Discovery session {SessionId} initialized. Scan range: {StartAddress:X} - {EndAddress:X}",
                    sessionId,
                    options.ScanStartAddress,
                    options.ScanStartAddress + options.ScanSize);
                    
                return Task.FromResult(Result.Success(session));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start discovery session {SessionId}", sessionId);
                return Task.FromResult(Result.Failure<DiscoverySession>(
                    $"Failed to start discovery session: {ex.Message}", ErrorType.Internal));
            }
        }
    }

    /// <summary>
    /// Stops an active discovery session and cleans up resources.
    /// </summary>
    public Task<Result> StopSessionAsync(DiscoverySession session, CancellationToken ct = default)
    {
        try
        {
            if (session == null)
                return Task.FromResult(Result.Failure("Session cannot be null", ErrorType.Validation));

            _logger.LogInformation("Stopping discovery session {SessionId}", session.SessionId);

            lock (_sessionLock)
            {
                if (!_activeSessions.TryGetValue(session.SessionId, out var context))
                    return Task.FromResult(Result.Failure("Session not found", ErrorType.NotFound));

                // Close process handle
                if (context.ProcessHandle != IntPtr.Zero)
                {
                    CloseHandle(context.ProcessHandle);
                    context.ProcessHandle = IntPtr.Zero;
                }

                context.Process?.Dispose();
                _activeSessions.Remove(session.SessionId);
            }

            session.IsActive = false;

            _logger.LogInformation("Discovery session {SessionId} stopped successfully", session.SessionId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping discovery session {SessionId}", session?.SessionId);
            return Task.FromResult(Result.Failure($"Failed to stop discovery session: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets the context for an active session.
    /// </summary>
    public DiscoverySessionContext? GetSessionContext(Guid sessionId)
    {
        lock (_sessionLock)
        {
            _activeSessions.TryGetValue(sessionId, out var context);
            return context;
        }
    }

    /// <summary>
    /// Checks if a session is active.
    /// </summary>
    public bool IsSessionActive(Guid sessionId)
    {
        lock (_sessionLock)
        {
            return _activeSessions.ContainsKey(sessionId);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sessionLock)
        {
            foreach (var context in _activeSessions.Values)
            {
                if (context.ProcessHandle != IntPtr.Zero)
                {
                    CloseHandle(context.ProcessHandle);
                }
                context.Process?.Dispose();
                context.Session.IsActive = false;
            }
            _activeSessions.Clear();
        }
    }
}

/// <summary>
/// Context for an active discovery session.
/// </summary>
public sealed class DiscoverySessionContext
{
    public required DiscoverySession Session { get; init; }
    public required IntPtr ProcessHandle { get; set; }
    public Process? Process { get; init; }
}
