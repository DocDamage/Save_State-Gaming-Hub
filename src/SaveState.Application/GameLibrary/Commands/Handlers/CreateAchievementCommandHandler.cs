namespace SaveState.Application.GameLibrary.Commands.Handlers;

using MediatR;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Handles the CreateAchievementCommand.
/// </summary>
public class CreateAchievementCommandHandler : IRequestHandler<CreateAchievementCommand, Guid>
{
    private readonly IAchievementRepository _achievementRepository;

    /// <summary>
    /// Initializes a new instance of the CreateAchievementCommandHandler.
    /// </summary>
    /// <param name="achievementRepository">The achievement repository.</param>
    public CreateAchievementCommandHandler(IAchievementRepository achievementRepository)
    {
        _achievementRepository = achievementRepository;
    }

    /// <summary>
    /// Handles the create achievement command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created achievement.</returns>
    public async Task<Guid> Handle(CreateAchievementCommand request, CancellationToken cancellationToken)
    {
        var achievement = new Achievement(
            request.Name,
            request.Description,
            request.IconPath,
            request.Points,
            request.Type);

        if (!string.IsNullOrEmpty(request.Criteria))
        {
            achievement.SetCriteria(request.Criteria);
        }

        await _achievementRepository.AddAchievementAsync(achievement, cancellationToken);

        return achievement.Id;
    }
}
