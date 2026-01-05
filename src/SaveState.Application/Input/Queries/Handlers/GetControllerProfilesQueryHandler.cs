using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Input;

namespace SaveState.Application.Input.Queries.Handlers;

/// <summary>
/// Handler for retrieving controller profiles.
/// </summary>
public class GetControllerProfilesQueryHandler : IRequestHandler<GetControllerProfilesQuery, Result<IReadOnlyList<ControllerProfileDto>>>
{
    private readonly IControllerProfileRepository _repository;
    private readonly ILogger<GetControllerProfilesQueryHandler> _logger;

    public GetControllerProfilesQueryHandler(
        IControllerProfileRepository repository,
        ILogger<GetControllerProfilesQueryHandler> _logger)
    {
        _repository = repository;
        this._logger = _logger;
    }

    public async Task<Result<IReadOnlyList<ControllerProfileDto>>> Handle(GetControllerProfilesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving controller profiles - GameId: {GameId}, Type: {Type}", request.GameId, request.Type);

            IReadOnlyList<Core.Input.Entities.ControllerProfile> profiles;

            // Apply filters
            if (request.GameId.HasValue)
            {
                profiles = await _repository.GetByGameIdAsync(request.GameId.Value, cancellationToken);

                // Include global profiles if requested
                if (request.IncludeGlobal)
                {
                    var globalProfiles = await _repository.GetAllAsync(cancellationToken);
                    var globalOnly = globalProfiles.Where(p => p.GameId == null).ToList();
                    profiles = profiles.Concat(globalOnly).ToList();
                }
            }
            else if (request.Type.HasValue)
            {
                profiles = await _repository.GetByTypeAsync(request.Type.Value, cancellationToken);
            }
            else
            {
                profiles = await _repository.GetAllAsync(cancellationToken);
            }

            // Map to DTOs
            var dtos = profiles.Select(p => new ControllerProfileDto(
                p.Id,
                p.Name,
                p.Type,
                p.GameId,
                p.ControllerId,
                p.GetMappings(),
                p.IsDefault,
                p.CreatedAt,
                p.LastUsedAt
            )).ToList();

            _logger.LogInformation("Retrieved {Count} controller profiles", dtos.Count);
            return Result<IReadOnlyList<ControllerProfileDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve controller profiles");
            return Result<IReadOnlyList<ControllerProfileDto>>.Failure($"Failed to retrieve controller profiles: {ex.Message}");
        }
    }
}
