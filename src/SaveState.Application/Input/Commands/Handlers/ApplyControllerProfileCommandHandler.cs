using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Input;
using SaveState.Core.Input.Services;

namespace SaveState.Application.Input.Commands.Handlers;

/// <summary>
/// Handler for applying a controller profile to the current session.
/// </summary>
public class ApplyControllerProfileCommandHandler : IRequestHandler<ApplyControllerProfileCommand, Result>
{
    private readonly IControllerProfileRepository _repository;
    private readonly IInputService _inputService;
    private readonly ILogger<ApplyControllerProfileCommandHandler> _logger;

    public ApplyControllerProfileCommandHandler(
        IControllerProfileRepository repository,
        IInputService inputService,
        ILogger<ApplyControllerProfileCommandHandler> logger)
    {
        _repository = repository;
        _inputService = inputService;
        _logger = logger;
    }

    public async Task<Result> Handle(ApplyControllerProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Applying controller profile: {ProfileId}", request.ProfileId);

            // Retrieve the profile
            var profile = await _repository.GetByIdAsync(request.ProfileId, cancellationToken);
            if (profile == null)
            {
                _logger.LogWarning("Controller profile not found: {ProfileId}", request.ProfileId);
                return Result.Failure($"Controller profile not found: {request.ProfileId}");
            }

            // Validate game context if specified
            if (request.GameId.HasValue && profile.GameId.HasValue && profile.GameId != request.GameId)
            {
                _logger.LogWarning("Profile {ProfileId} is for game {ProfileGameId}, but requested for {RequestedGameId}",
                    request.ProfileId, profile.GameId, request.GameId);
                return Result.Failure("Profile is not configured for this game");
            }

            // Apply the mappings via the input service
            var mappings = profile.GetMappings();
            var applyResult = await _inputService.ApplyControllerMappingsAsync(mappings, cancellationToken);

            if (!applyResult.IsSuccess)
            {
                _logger.LogError("Failed to apply controller mappings: {Error}", applyResult.Error);
                return Result.Failure($"Failed to apply controller mappings: {applyResult.Error}");
            }

            // Record usage
            profile.RecordUsage();
            await _repository.UpdateAsync(profile, cancellationToken);

            _logger.LogInformation("Controller profile applied successfully: {ProfileId}", request.ProfileId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply controller profile: {ProfileId}", request.ProfileId);
            return Result.Failure($"Failed to apply controller profile: {ex.Message}");
        }
    }
}
