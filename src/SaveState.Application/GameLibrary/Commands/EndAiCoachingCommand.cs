namespace SaveState.Application.GameLibrary.Commands;

using MediatR;
using SaveState.Core.Common;

public record EndAiCoachingCommand(Guid SessionId) : IRequest<Result>;