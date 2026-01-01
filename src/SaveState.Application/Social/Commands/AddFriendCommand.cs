namespace SaveState.Application.Social.Commands;

using MediatR;
using SaveState.Core.Common;

public record AddFriendCommand(Guid FriendId) : IRequest<Result>;