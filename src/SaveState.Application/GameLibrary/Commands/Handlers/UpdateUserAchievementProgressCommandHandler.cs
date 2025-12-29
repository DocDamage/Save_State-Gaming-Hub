namespace SaveState.Application.GameLibrary.Commands.Handlers;

using MediatR;
using SaveState.Core.GameLibrary.Services;

/// <summary>
/// Handles the UpdateUserAchievementProgressCommand.
/// </summary>
public class UpdateUserAchievementProgressCommandHandler : IRequestHandler<UpdateUserAchievementProgressCommand, Unit>
{
    private readonly IAchievementService _achievementService;

    /// <summary>
    /// Initializes a new instance of the UpdateUserAchievementProgressCommandHandler.
    /// </summary>
    /// <param name="achievementService">The achievement service.</param>
    public UpdateUserAchievementProgressCommandHandler(IAchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    /// <summary>
    /// Handles the update progress command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit result.</returns>
    public async Task<Unit> Handle(UpdateUserAchievementProgressCommand request, CancellationToken cancellationToken)
    {
        await _achievementService.UpdateProgressAsync(
            request.UserId,
            request.AchievementType,
            request.ProgressIncrement,
            request.Metadata,
            cancellationToken);

        return Unit.Value;
    }
}
