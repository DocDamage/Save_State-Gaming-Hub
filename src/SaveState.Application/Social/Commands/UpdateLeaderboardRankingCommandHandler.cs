using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Social.Repositories;
using SaveState.Core.UserManagement.Services;

namespace SaveState.Application.Social.Commands;

/// <summary>
/// Handler for updating leaderboard rankings.
/// </summary>
public class UpdateLeaderboardRankingCommandHandler : IRequestHandler<UpdateLeaderboardRankingCommand, Result>
{
    private readonly ICommunityRepository _communityRepository;
    private readonly IUserService _userService;
    private readonly ILogger<UpdateLeaderboardRankingCommandHandler> _logger;

    public UpdateLeaderboardRankingCommandHandler(
        ICommunityRepository communityRepository,
        IUserService userService,
        ILogger<UpdateLeaderboardRankingCommandHandler> logger)
    {
        _communityRepository = communityRepository;
        _userService = userService;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateLeaderboardRankingCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the leaderboard
            var leaderboardResult = await _communityRepository.GetLeaderboardAsync(request.LeaderboardId, cancellationToken);
            if (leaderboardResult.IsFailure || leaderboardResult.Value == null)
            {
                _logger.LogWarning("Leaderboard {LeaderboardId} not found", request.LeaderboardId);
                return Result.Failure($"Leaderboard {request.LeaderboardId} not found");
            }

            var leaderboard = leaderboardResult.Value;

            // Check if user already has a ranking
            var existingRanking = leaderboard.Entries.FirstOrDefault(e => e.UserId == request.UserId);

            if (existingRanking != null)
            {
                // Update existing ranking if new score is better
                if (request.Score > existingRanking.Score)
                {
                    existingRanking.Score = request.Score;
                    existingRanking.Metadata = request.Metadata;
                    existingRanking.LastUpdated = DateTime.UtcNow;

                    await _communityRepository.UpdateLeaderboardEntryAsync(existingRanking, cancellationToken);
                    _logger.LogInformation("Updated leaderboard ranking for user {UserId} on leaderboard {LeaderboardId} with score {Score}",
                        request.UserId, request.LeaderboardId, request.Score);
                }
                else
                {
                    _logger.LogDebug("Score {NewScore} not better than existing score {ExistingScore} for user {UserId}",
                        request.Score, existingRanking.Score, request.UserId);
                }
            }
            else
            {
                // Create new ranking with username from user service
                var usernameResult = await _userService.GetUsernameAsync(request.UserId, cancellationToken);
                var userName = usernameResult.IsSuccess ? usernameResult.Value! : $"User-{request.UserId.ToString().Substring(0, 8)}";

                var newRanking = new SaveState.Core.Social.Entities.LeaderboardRanking
                {
                    Id = Guid.NewGuid(),
                    LeaderboardId = request.LeaderboardId,
                    UserId = request.UserId,
                    UserName = userName,
                    Score = request.Score,
                    Rank = 0, // Will be calculated when sorting entries
                    Metadata = request.Metadata,
                    LastUpdated = DateTime.UtcNow
                };

                leaderboard.Entries.Add(newRanking);
                await _communityRepository.UpdateLeaderboardAsync(leaderboard, cancellationToken);

                _logger.LogInformation("Created new leaderboard ranking for user {UserId} on leaderboard {LeaderboardId} with score {Score}",
                    request.UserId, request.LeaderboardId, request.Score);
            }

            // Recalculate ranks
            var sortedEntries = leaderboard.Entries.OrderByDescending(e => e.Score).ToList();
            for (int i = 0; i < sortedEntries.Count; i++)
            {
                sortedEntries[i].Rank = i + 1;
            }

            await _communityRepository.UpdateLeaderboardAsync(leaderboard, cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leaderboard ranking for user {UserId} on leaderboard {LeaderboardId}",
                request.UserId, request.LeaderboardId);
            return Result.Failure($"Failed to update leaderboard ranking: {ex.Message}");
        }
    }
}
