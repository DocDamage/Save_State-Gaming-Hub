using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Automation.Services;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common;

namespace SaveState.Application.Automation.Commands.Handlers;

/// <summary>
/// Handler for macro recording commands.
/// </summary>
public class StartMacroRecordingCommandHandler :
    IRequestHandler<StartMacroRecordingCommand, Result<MacroRecordingSession>>
{
    private readonly IMacroRecorder _macroRecorder;
    private readonly ILogger<StartMacroRecordingCommandHandler> _logger;

    public StartMacroRecordingCommandHandler(
        IMacroRecorder macroRecorder,
        ILogger<StartMacroRecordingCommandHandler> logger)
    {
        _macroRecorder = macroRecorder;
        _logger = logger;
    }

    public async Task<Result<MacroRecordingSession>> Handle(
        StartMacroRecordingCommand request,
        CancellationToken ct)
    {
        try
        {
            var config = new MacroRecordingConfig(
                GameId: request.GameId,
                Name: request.Name,
                Description: request.Description,
                Mode: request.Mode);

            var result = await _macroRecorder.StartRecordingAsync(config, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Started macro recording for game {GameId}", request.GameId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start macro recording for game {GameId}", request.GameId);
            return Result<MacroRecordingSession>.Failure($"Failed to start recording: {ex.Message}");
        }
    }
}

public class StopMacroRecordingCommandHandler :
    IRequestHandler<StopMacroRecordingCommand, Result<Macro>>
{
    private readonly IMacroRecorder _macroRecorder;
    private readonly IMacroManager _macroManager;
    private readonly ILogger<StopMacroRecordingCommandHandler> _logger;

    public StopMacroRecordingCommandHandler(
        IMacroRecorder macroRecorder,
        IMacroManager macroManager,
        ILogger<StopMacroRecordingCommandHandler> logger)
    {
        _macroRecorder = macroRecorder;
        _macroManager = macroManager;
        _logger = logger;
    }

    public async Task<Result<Macro>> Handle(
        StopMacroRecordingCommand request,
        CancellationToken ct)
    {
        try
        {
            var stopResult = await _macroRecorder.StopRecordingAsync(request.SessionId, ct);
            if (!stopResult.IsSuccess)
            {
                return Result<Macro>.Failure(stopResult.Error!);
            }

            // The StopRecordingAsync already creates and returns the macro
            var macro = stopResult.Value!;
            _logger.LogInformation("Stopped macro recording session {SessionId}, created macro {MacroId}",
                request.SessionId, macro.Id);

            return Result<Macro>.Success(macro);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop macro recording session {SessionId}", request.SessionId);
            return Result<Macro>.Failure($"Failed to stop recording: {ex.Message}");
        }
    }
}

public class CancelMacroRecordingCommandHandler :
    IRequestHandler<CancelMacroRecordingCommand, Result>
{
    private readonly IMacroRecorder _macroRecorder;
    private readonly ILogger<CancelMacroRecordingCommandHandler> _logger;

    public CancelMacroRecordingCommandHandler(
        IMacroRecorder macroRecorder,
        ILogger<CancelMacroRecordingCommandHandler> logger)
    {
        _macroRecorder = macroRecorder;
        _logger = logger;
    }

    public async Task<Result> Handle(
        CancelMacroRecordingCommand request,
        CancellationToken ct)
    {
        try
        {
            var result = await _macroRecorder.CancelRecordingAsync(request.SessionId, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Cancelled macro recording session {SessionId}", request.SessionId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel macro recording session {SessionId}", request.SessionId);
            return Result.Failure($"Failed to cancel recording: {ex.Message}");
        }
    }
}

public class StartMacroPlaybackCommandHandler :
    IRequestHandler<StartMacroPlaybackCommand, Result<MacroPlaybackSession>>
{
    private readonly IMacroPlayer _macroPlayer;
    private readonly ILogger<StartMacroPlaybackCommandHandler> _logger;

    public StartMacroPlaybackCommandHandler(
        IMacroPlayer macroPlayer,
        ILogger<StartMacroPlaybackCommandHandler> logger)
    {
        _macroPlayer = macroPlayer;
        _logger = logger;
    }

    public async Task<Result<MacroPlaybackSession>> Handle(
        StartMacroPlaybackCommand request,
        CancellationToken ct)
    {
        try
        {
            var result = await _macroPlayer.StartPlaybackAsync(request.MacroId, request.Config, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Started macro playback for macro {MacroId}", request.MacroId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start macro playback for macro {MacroId}", request.MacroId);
            return Result<MacroPlaybackSession>.Failure($"Failed to start playback: {ex.Message}");
        }
    }
}

public class StopMacroPlaybackCommandHandler :
    IRequestHandler<StopMacroPlaybackCommand, Result>
{
    private readonly IMacroPlayer _macroPlayer;
    private readonly ILogger<StopMacroPlaybackCommandHandler> _logger;

    public StopMacroPlaybackCommandHandler(
        IMacroPlayer macroPlayer,
        ILogger<StopMacroPlaybackCommandHandler> logger)
    {
        _macroPlayer = macroPlayer;
        _logger = logger;
    }

    public async Task<Result> Handle(
        StopMacroPlaybackCommand request,
        CancellationToken ct)
    {
        try
        {
            var result = await _macroPlayer.StopPlaybackAsync(request.SessionId, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Stopped macro playback session {SessionId}", request.SessionId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop macro playback session {SessionId}", request.SessionId);
            return Result.Failure($"Failed to stop playback: {ex.Message}");
        }
    }
}

public class GetMacroCommandHandler :
    IRequestHandler<GetMacroCommand, Result<Macro>>
{
    private readonly IMacroManager _macroManager;
    private readonly ILogger<GetMacroCommandHandler> _logger;

    public GetMacroCommandHandler(
        IMacroManager macroManager,
        ILogger<GetMacroCommandHandler> logger)
    {
        _macroManager = macroManager;
        _logger = logger;
    }

    public async Task<Result<Macro>> Handle(
        GetMacroCommand request,
        CancellationToken ct)
    {
        try
        {
            var result = await _macroManager.GetMacroAsync(request.MacroId, ct);

            if (result.IsSuccess)
            {
                _logger.LogDebug("Retrieved macro {MacroId}", request.MacroId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get macro {MacroId}", request.MacroId);
            return Result<Macro>.Failure($"Failed to get macro: {ex.Message}");
        }
    }
}

public class GetMacrosForGameCommandHandler :
    IRequestHandler<GetMacrosForGameCommand, Result<IReadOnlyList<Macro>>>
{
    private readonly IMacroManager _macroManager;
    private readonly ILogger<GetMacrosForGameCommandHandler> _logger;

    public GetMacrosForGameCommandHandler(
        IMacroManager macroManager,
        ILogger<GetMacrosForGameCommandHandler> logger)
    {
        _macroManager = macroManager;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<Macro>>> Handle(
        GetMacrosForGameCommand request,
        CancellationToken ct)
    {
        try
        {
            var result = await _macroManager.GetMacrosForGameAsync(request.GameId, ct);

            if (result.IsSuccess)
            {
                _logger.LogDebug("Retrieved {Count} macros for game {GameId}",
                    result.Value!.Count, request.GameId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get macros for game {GameId}", request.GameId);
            return Result<IReadOnlyList<Macro>>.Failure($"Failed to get macros: {ex.Message}");
        }
    }
}

public class DeleteMacroCommandHandler :
    IRequestHandler<DeleteMacroCommand, Result>
{
    private readonly IMacroManager _macroManager;
    private readonly ILogger<DeleteMacroCommandHandler> _logger;

    public DeleteMacroCommandHandler(
        IMacroManager macroManager,
        ILogger<DeleteMacroCommandHandler> logger)
    {
        _macroManager = macroManager;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteMacroCommand request,
        CancellationToken ct)
    {
        try
        {
            var result = await _macroManager.DeleteMacroAsync(request.MacroId, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Deleted macro {MacroId}", request.MacroId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete macro {MacroId}", request.MacroId);
            return Result.Failure($"Failed to delete macro: {ex.Message}");
        }
    }
}