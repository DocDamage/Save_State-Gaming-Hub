namespace SaveState.Application.Social.Commands;

using MediatR;
using SaveState.Core.Common;

public record RemoveFriendCommand(Guid FriendId) : IRequest<Result>;