using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Social.Commands;

/// <summary>
/// Command to update a user's leaderboard ranking.
/// </summary>
public record UpdateLeaderboardRankingCommand(
    Guid LeaderboardId,
    Guid UserId,
    int Score,
    string? Metadata = null) : IRequest<Result>;
