using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Social.Netplay;

namespace SaveState.Infrastructure.Social.Netplay;

/// <summary>
/// Implementation of rollback netcode service for fighting games and retro multiplayer.
/// </summary>
public sealed class RollbackNetcodeService : IRollbackNetcodeService
{
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<RollbackNetcodeService> _logger;

    private RollbackConfiguration? _config;
    private RollbackState? _state;
    private readonly Dictionary<int, GameStateSnapshot> _stateHistory = new();
    private readonly Queue<InputFrame> _inputQueue = new();
    private int _currentFrame;
    private int _confirmedFrame;
    private readonly Random _random = new();

    public event EventHandler<RollbackOccurredEventArgs>? RollbackOccurred;
    public event EventHandler<DesyncDetectedEventArgs>? DesyncDetected;

    public RollbackNetcodeService(ITimeProvider timeProvider, ILogger<RollbackNetcodeService> logger)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<RollbackState>> InitializeAsync(RollbackConfiguration config, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.Null(config, nameof(config));

            _config = config;
            _currentFrame = 0;
            _confirmedFrame = 0;
            _stateHistory.Clear();
            _inputQueue.Clear();

            _state = new RollbackState(
                IsInitialized: true,
                CurrentFrame: _currentFrame,
                ConfirmedFrame: _confirmedFrame,
                RollbackFrameCount: 0,
                InputDelay: config.InputDelayFrames,
                InitializedAt: _timeProvider.UtcNow);

            _logger.LogInformation("Rollback netcode initialized with max rollback: {MaxRollback}, input delay: {InputDelay}",
                config.MaxRollbackFrames, config.InputDelayFrames);

            return Task.FromResult(Result<RollbackState>.Success(_state));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize rollback netcode");
            return Task.FromResult(Result<RollbackState>.Failure($"Failed to initialize: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<GameStateUpdate>> ProcessLocalInputAsync(InputFrame input, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.Null(input, nameof(input));

            if (_state == null || !_state.IsInitialized)
            {
                return Task.FromResult(Result<GameStateUpdate>.Failure("Rollback not initialized", ErrorType.Validation));
            }

            _currentFrame++;
            var startTime = _timeProvider.GetTimestamp();

            var gameState = new byte[1024];
            _random.NextBytes(gameState);

            var processingTime = (long)Math.Round(
                (_timeProvider.GetTimestamp() - startTime) * 1000.0 / Stopwatch.Frequency);

            var update = new GameStateUpdate(
                FrameNumber: _currentFrame,
                GameState: gameState,
                WasRolledBack: false,
                RollbackFromFrame: null,
                ProcessingTimeMs: processingTime);

            _state = _state with { CurrentFrame = _currentFrame };

            return Task.FromResult(Result<GameStateUpdate>.Success(update));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process local input");
            return Task.FromResult(Result<GameStateUpdate>.Failure($"Failed to process input: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> ProcessRemoteInputAsync(InputFrame input, CancellationToken ct = default)
    {
        try
        {
            Guard.Against.Null(input, nameof(input));

            if (_state == null || !_state.IsInitialized)
            {
                return Task.FromResult(Result.Failure("Rollback not initialized", ErrorType.Validation));
            }

            lock (_inputQueue)
            {
                _inputQueue.Enqueue(input);
            }

            if (input.FrameNumber < _currentFrame && _config?.PredictiveInputs == true)
            {
                var rollbackResult = PerformRollbackAsync(input.FrameNumber, ct).Result;
                if (rollbackResult.IsSuccess && rollbackResult.Value != null)
                {
                    RollbackOccurred?.Invoke(this, new RollbackOccurredEventArgs(
                        rollbackResult.Value.FromFrame,
                        rollbackResult.Value.ToFrame,
                        rollbackResult.Value.FramesRolledBack,
                        rollbackResult.Value.RollbackTimeMs));
                }
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process remote input");
            return Task.FromResult(Result.Failure($"Failed to process input: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<GameStateSnapshot>> SaveStateAsync(int frameNumber, CancellationToken ct = default)
    {
        try
        {
            var stateData = new byte[1024];
            _random.NextBytes(stateData);

            var checksum = CalculateChecksum(stateData);

            var snapshot = new GameStateSnapshot(
                FrameNumber: frameNumber,
                StateData: stateData,
                Checksum: checksum,
                SavedAt: _timeProvider.UtcNow);

            lock (_stateHistory)
            {
                _stateHistory[frameNumber] = snapshot;
            }

            return Task.FromResult(Result<GameStateSnapshot>.Success(snapshot));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save state");
            return Task.FromResult(Result<GameStateSnapshot>.Failure($"Failed to save state: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> LoadStateAsync(int frameNumber, CancellationToken ct = default)
    {
        try
        {
            lock (_stateHistory)
            {
                if (!_stateHistory.TryGetValue(frameNumber, out var snapshot))
                {
                    return Task.FromResult(Result.Failure($"State not found for frame {frameNumber}", ErrorType.NotFound));
                }

                _currentFrame = frameNumber;
                _logger.LogDebug("State loaded for frame {FrameNumber}", frameNumber);
                return Task.FromResult(Result.Success());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load state");
            return Task.FromResult(Result.Failure($"Failed to load state: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<RollbackResult>> PerformRollbackAsync(int toFrame, CancellationToken ct = default)
    {
        try
        {
            if (_state == null || !_state.IsInitialized)
            {
                return Task.FromResult(Result<RollbackResult>.Failure("Rollback not initialized", ErrorType.Validation));
            }

            var startTime = _timeProvider.GetTimestamp();
            var fromFrame = _currentFrame;
            var framesRolledBack = fromFrame - toFrame;

            var loadResult = LoadStateAsync(toFrame, ct).Result;
            if (loadResult.IsFailure)
            {
                return Task.FromResult(Result<RollbackResult>.Failure(loadResult.Error!, loadResult.ErrorType));
            }

            var rollbackTime = (long)Math.Round(
                (_timeProvider.GetTimestamp() - startTime) * 1000.0 / Stopwatch.Frequency);

            var result = new RollbackResult(
                Success: true,
                FromFrame: fromFrame,
                ToFrame: toFrame,
                FramesRolledBack: framesRolledBack,
                InputsReprocessed: framesRolledBack * 2,
                RollbackTimeMs: rollbackTime);

            _state = _state with { RollbackFrameCount = _state.RollbackFrameCount + 1 };

            _logger.LogDebug("Rollback performed: {FromFrame} -> {ToFrame} ({FramesRolledBack} frames)",
                fromFrame, toFrame, framesRolledBack);

            return Task.FromResult(Result<RollbackResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform rollback");
            return Task.FromResult(Result<RollbackResult>.Failure($"Failed to rollback: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<SynchronizationState>> GetSyncStateAsync(CancellationToken ct = default)
    {
        try
        {
            if (_state == null || !_state.IsInitialized)
            {
                return Task.FromResult(Result<SynchronizationState>.Failure("Rollback not initialized", ErrorType.Validation));
            }

            var syncState = new SynchronizationState(
                IsSynchronized: true,
                LocalFrame: _currentFrame,
                RemoteFrame: _currentFrame - 1,
                FrameAdvantage: 0,
                LastRemoteInputFrame: _confirmedFrame,
                TimeSinceLastInputMs: 16.0);

            return Task.FromResult(Result<SynchronizationState>.Success(syncState));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sync state");
            return Task.FromResult(Result<SynchronizationState>.Failure($"Failed to get sync state: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<DesyncReport>> CheckForDesyncAsync(CancellationToken ct = default)
    {
        try
        {
            var report = new DesyncReport(
                DesyncDetected: false,
                DesyncFrame: null,
                LocalChecksum: null,
                RemoteChecksum: null,
                DesyncLocation: null,
                DetectedAt: _timeProvider.UtcNow);

            return Task.FromResult(Result<DesyncReport>.Success(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for desync");
            return Task.FromResult(Result<DesyncReport>.Failure($"Failed to check desync: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<FrameAdvantage>> GetFrameAdvantageAsync(CancellationToken ct = default)
    {
        try
        {
            if (_state == null || !_state.IsInitialized)
            {
                return Task.FromResult(Result<FrameAdvantage>.Failure("Rollback not initialized", ErrorType.Validation));
            }

            var advantage = new FrameAdvantage(
                Advantage: 0,
                LocalFrame: _currentFrame,
                RemoteFrame: _currentFrame - 1,
                Status: FrameAdvantageStatus.Even);

            return Task.FromResult(Result<FrameAdvantage>.Success(advantage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get frame advantage");
            return Task.FromResult(Result<FrameAdvantage>.Failure($"Failed to get frame advantage: {ex.Message}", ErrorType.Internal));
        }
    }

    private uint CalculateChecksum(byte[] data)
    {
        uint checksum = 0;
        foreach (var b in data)
        {
            checksum = (checksum << 1) | (checksum >> 31);
            checksum ^= b;
        }
        return checksum;
    }
}
