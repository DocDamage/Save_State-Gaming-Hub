namespace SaveState.Application.Social.Queries;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Services;

public record GetLeaderboardQuery(
    LeaderboardType Type,
    string? GameId = null,
    int Limit = 50) : IRequest<Result<IReadOnlyList<LeaderboardEntry>>>;