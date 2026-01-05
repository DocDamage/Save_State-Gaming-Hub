using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Services;

namespace SaveState.Application.SaveStates.Commands.Handlers;

public class ConfigureAutoSaveCommandHandler : IRequestHandler<ConfigureAutoSaveCommand, Result>
{
    private readonly IAutoSaveManager _autoSaveManager;
    private readonly ILogger<ConfigureAutoSaveCommandHandler> _logger;

    public ConfigureAutoSaveCommandHandler(IAutoSaveManager autoSaveManager, ILogger<ConfigureAutoSaveCommandHandler> logger)
    {
        _autoSaveManager = autoSaveManager;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfigureAutoSaveCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _autoSaveManager.ConfigureAutoSaveAsync(request.GameId, request.Config, ct);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Auto-save configured for game {GameId}", request.GameId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure auto-save for game {GameId}", request.GameId);
            return Result.Failure($"Failed to configure auto-save: {ex.Message}");
        }
    }
}

public class EnableAutoSaveCommandHandler : IRequestHandler<EnableAutoSaveCommand, Result>
{
    private readonly IAutoSaveManager _autoSaveManager;
    private readonly ILogger<EnableAutoSaveCommandHandler> _logger;

    public EnableAutoSaveCommandHandler(IAutoSaveManager autoSaveManager, ILogger<EnableAutoSaveCommandHandler> logger)
    {
        _autoSaveManager = autoSaveManager;
        _logger = logger;
    }

    public async Task<Result> Handle(EnableAutoSaveCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _autoSaveManager.EnableAutoSaveAsync(request.GameId, ct);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Auto-save enabled for game {GameId}", request.GameId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable auto-save for game {GameId}", request.GameId);
            return Result.Failure($"Failed to enable auto-save: {ex.Message}");
        }
    }
}

public class DisableAutoSaveCommandHandler : IRequestHandler<DisableAutoSaveCommand, Result>
{
    private readonly IAutoSaveManager _autoSaveManager;
    private readonly ILogger<DisableAutoSaveCommandHandler> _logger;

    public DisableAutoSaveCommandHandler(IAutoSaveManager autoSaveManager, ILogger<DisableAutoSaveCommandHandler> logger)
    {
        _autoSaveManager = autoSaveManager;
        _logger = logger;
    }

    public async Task<Result> Handle(DisableAutoSaveCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _autoSaveManager.DisableAutoSaveAsync(request.GameId, ct);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Auto-save disabled for game {GameId}", request.GameId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable auto-save for game {GameId}", request.GameId);
            return Result.Failure($"Failed to disable auto-save: {ex.Message}");
        }
    }
}

public class TriggerAutoSaveCommandHandler : IRequestHandler<TriggerAutoSaveCommand, Result>
{
    private readonly IAutoSaveManager _autoSaveManager;
    private readonly ILogger<TriggerAutoSaveCommandHandler> _logger;

    public TriggerAutoSaveCommandHandler(IAutoSaveManager autoSaveManager, ILogger<TriggerAutoSaveCommandHandler> logger)
    {
        _autoSaveManager = autoSaveManager;
        _logger = logger;
    }

    public async Task<Result> Handle(TriggerAutoSaveCommand request, CancellationToken ct)
    {
        try
        {
            var result = await _autoSaveManager.TriggerSaveAsync(request.GameId, request.Trigger, ct);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Auto-save triggered for game {GameId} with trigger {Trigger}", request.GameId, request.Trigger);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger auto-save for game {GameId}", request.GameId);
            return Result.Failure($"Failed to trigger auto-save: {ex.Message}");
        }
    }
}

public class GetAutoSaveStatusCommandHandler : IRequestHandler<GetAutoSaveStatusCommand, Result<AutoSaveStatus>>
{
    private readonly IAutoSaveManager _autoSaveManager;
    private readonly ILogger<GetAutoSaveStatusCommandHandler> _logger;

    public GetAutoSaveStatusCommandHandler(IAutoSaveManager autoSaveManager, ILogger<GetAutoSaveStatusCommandHandler> logger)
    {
        _autoSaveManager = autoSaveManager;
        _logger = logger;
    }

    public async Task<Result<AutoSaveStatus>> Handle(GetAutoSaveStatusCommand request, CancellationToken ct)
    {
        try
        {
            return await _autoSaveManager.GetAutoSaveStatusAsync(request.GameId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get auto-save status for game {GameId}", request.GameId);
            return Result<AutoSaveStatus>.Failure($"Failed to get auto-save status: {ex.Message}");
        }
    }
}
