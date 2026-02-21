using SaveState.Core.Common;
using SaveState.Core.Netplay.Models;
using SaveState.Core.Netplay.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.Netplay.Services;

public class RollbackNetcodeWrapper : IRollbackNetcodeWrapper
{
    private readonly ILogger<RollbackNetcodeWrapper> _logger;
    private NetplaySession? _currentSession;
    private int _currentFrame;
    private bool _isRunning;

    public RollbackNetcodeWrapper(ILogger<RollbackNetcodeWrapper> logger)
    {
        _logger = logger;
    }

    public Task<Result> InitializeAsync(NetplaySession session, CancellationToken ct = default)
    {
        _currentSession = session;
        _currentFrame = 0;
        _logger.LogInformation(
            "Initialized rollback netcode for session {SessionId} with input delay {InputDelay} and max rollback {MaxRollback}",
            session.SessionId, session.RollbackConfig.InputDelay, session.RollbackConfig.MaxRollbackFrames);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StartSessionAsync(CancellationToken ct = default)
    {
        if (_currentSession is null)
        {
            return Task.FromResult(Result.Failure("No session initialized", ErrorType.Validation));
        }

        _isRunning = true;
        _logger.LogInformation("Started rollback netcode session");
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopSessionAsync(CancellationToken ct = default)
    {
        _isRunning = false;
        _logger.LogInformation("Stopped rollback netcode session");
        return Task.FromResult(Result.Success());
    }

    public Task<Result> UpdateInputAsync(int playerIndex, byte[] inputs, CancellationToken ct = default)
    {
        if (!_isRunning)
        {
            return Task.FromResult(Result.Failure("Session not running", ErrorType.Validation));
        }

        // Simulate input processing
        _logger.LogDebug("Updated input for player {PlayerIndex} on frame {Frame}", playerIndex, _currentFrame);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<byte[]>> GetConfirmedInputsAsync(int frame, CancellationToken ct = default)
    {
        if (!_isRunning)
        {
            return Task.FromResult(Result<byte[]>.Failure("Session not running", ErrorType.Validation));
        }

        // Return simulated confirmed inputs
        var inputs = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        return Task.FromResult(Result<byte[]>.Success(inputs));
    }

    public Task<Result> SetRollbackConfigAsync(RollbackConfig config, CancellationToken ct = default)
    {
        if (_currentSession is null)
        {
            return Task.FromResult(Result.Failure("No session initialized", ErrorType.Validation));
        }

        _currentSession = _currentSession with { RollbackConfig = config };
        _logger.LogInformation(
            "Updated rollback config: InputDelay={InputDelay}, MaxRollbackFrames={MaxRollbackFrames}",
            config.InputDelay, config.MaxRollbackFrames);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<int>> GetCurrentFrameAsync(CancellationToken ct = default)
    {
        if (!_isRunning)
        {
            return Task.FromResult(Result<int>.Failure("Session not running", ErrorType.Validation));
        }

        return Task.FromResult(Result<int>.Success(_currentFrame));
    }

    public Task<Result<int>> GetPingAsync(CancellationToken ct = default)
    {
        if (!_isRunning)
        {
            return Task.FromResult(Result<int>.Failure("Session not running", ErrorType.Validation));
        }

        // Simulate ping calculation
        var ping = new Random().Next(20, 150);
        return Task.FromResult(Result<int>.Success(ping));
    }
}
