namespace SaveState.Application.Social.Queries;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

public record GetFriendsQuery() : IRequest<Result<IReadOnlyList<Friend>>>;