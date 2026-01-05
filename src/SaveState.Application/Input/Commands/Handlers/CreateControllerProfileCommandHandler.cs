using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Input;
using SaveState.Core.Input.Entities;

namespace SaveState.Application.Input.Commands.Handlers;

/// <summary>
/// Handler for creating a new controller profile.
/// </summary>
public class CreateControllerProfileCommandHandler : IRequestHandler<CreateControllerProfileCommand, Result<Guid>>
{
    private readonly IControllerProfileRepository _repository;
    private readonly ILogger<CreateControllerProfileCommandHandler> _logger;

    public CreateControllerProfileCommandHandler(
        IControllerProfileRepository repository,
        ILogger<CreateControllerProfileCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateControllerProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating controller profile: {Name} for type {Type}", request.Name, request.Type);

            // Create the profile entity
            var profile = ControllerProfile.Create(request.Name, request.Type, request.GameId);

            // Set optional properties
            if (request.ControllerId != null)
            {
                profile.SetControllerId(request.ControllerId);
            }

            if (request.ButtonMappings != null && request.ButtonMappings.Count > 0)
            {
                profile.SetMappings(request.ButtonMappings);
            }

            // Persist to repository
            await _repository.AddAsync(profile, cancellationToken);

            _logger.LogInformation("Controller profile created successfully: {ProfileId}", profile.Id);
            return Result<Guid>.Success(profile.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create controller profile: {Name}", request.Name);
            return Result<Guid>.Failure($"Failed to create controller profile: {ex.Message}");
        }
    }
}
